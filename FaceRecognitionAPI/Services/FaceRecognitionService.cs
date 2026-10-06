using System.Text.Json;
using FaceRecognitionAPI.Data;
using FaceRecognitionAPI.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FaceRecognitionAPI.Services;

public class FaceRecognitionService
{
    private readonly PythonFaceService _pythonFaceService;
    private readonly ApplicationDbContext _context;

    public FaceRecognitionService(
        PythonFaceService pythonFaceService,
        ApplicationDbContext context)
    {
        _pythonFaceService = pythonFaceService;
        _context = context;
    }

    private async Task<Guid> FindMatchingCandidateIdAsync(
        float[] embedding)
    {
        const double similarityThreshold = 0.75;

        var candidateIds =
            await _pythonFaceService.SearchCandidatesAsync(
                embedding);

        Console.WriteLine(
            $"Candidate IDs: {string.Join(", ", candidateIds)}");

        if (candidateIds.Count == 0)
        {
            return Guid.Empty;
        }

        var candidateEmbeddings =
            await _context.FaceEmbeddings
                .AsNoTracking()
                .Where(f => candidateIds.Contains(f.CandidateId))
                .ToListAsync();

        Guid bestMatchingCandidateId = Guid.Empty;
        double highestSimilarity = double.MinValue;

        foreach (var storedFace in candidateEmbeddings)
        {
            var storedEmbedding =
                JsonSerializer.Deserialize<float[]>(
                    storedFace.Embedding);

            if (storedEmbedding == null ||
                storedEmbedding.Length != embedding.Length)
            {
                continue;
            }

            double similarity =
                CalculateCosineSimilarity(
                    embedding,
                    storedEmbedding);

            Console.WriteLine(
                $"CandidateId: {storedFace.CandidateId}, " +
                $"Similarity: {similarity}");

            if (similarity > highestSimilarity)
            {
                highestSimilarity = similarity;
                bestMatchingCandidateId =
                    storedFace.CandidateId;
            }
        }

        return highestSimilarity >= similarityThreshold
            ? bestMatchingCandidateId
            : Guid.Empty;
    }

    private static double CalculateCosineSimilarity(
        float[] firstEmbedding,
        float[] secondEmbedding)
    {
        double dotProduct = 0;
        double firstMagnitude = 0;
        double secondMagnitude = 0;

        for (int i = 0; i < firstEmbedding.Length; i++)
        {
            dotProduct +=
                firstEmbedding[i] *
                secondEmbedding[i];

            firstMagnitude +=
                firstEmbedding[i] *
                firstEmbedding[i];

            secondMagnitude +=
                secondEmbedding[i] *
                secondEmbedding[i];
        }

        if (firstMagnitude == 0 ||
            secondMagnitude == 0)
        {
            return 0;
        }

        return dotProduct /
            (Math.Sqrt(firstMagnitude) *
             Math.Sqrt(secondMagnitude));
    }

    public async Task RegisterFaceAsync(
        FaceRegistrationRequest request)
    {
        if (request.CandidateId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Candidate ID is required.");
        }

        var candidateExists =
            await _context.Candidates
                .AnyAsync(c =>
                    c.Id == request.CandidateId);

        if (!candidateExists)
        {
            throw new InvalidOperationException(
                "Candidate account was not found.");
        }

        var embedding =
            await _pythonFaceService
                .GenerateEmbeddingAsync(request.Image);

        var matchingCandidateId =
            await FindMatchingCandidateIdAsync(
                embedding);

        if (matchingCandidateId != Guid.Empty && matchingCandidateId != request.CandidateId)
        {
            throw new InvalidOperationException(
                "This face is already linked to another account.");
        }

        var existingRegistration =
            await _context.FaceEmbeddings
                .FirstOrDefaultAsync(f =>
                    f.CandidateId == request.CandidateId);

        if (existingRegistration != null)
        {
            existingRegistration.Embedding = JsonSerializer.Serialize(embedding);
            await _context.SaveChangesAsync();
        }
        else
        {
            var faceEmbedding = new ResumeAnalysis.Api.Entities.FaceEmbedding
            {
                CandidateId = request.CandidateId,
                Embedding = JsonSerializer.Serialize(embedding)
            };

            _context.FaceEmbeddings.Add(faceEmbedding);
            await _context.SaveChangesAsync();
        }

        try
        {
            await _pythonFaceService.AddEmbeddingToIndexAsync(
                request.CandidateId,
                embedding);
        }
        catch
        {
            // If already in index or index needs refresh, handled by AnnIndexSyncHostedService
        }
    }

    public async Task<Guid> LoginWithFaceAsync(
        FaceLoginRequest request)
    {
        var embedding =
            await _pythonFaceService
                .GenerateEmbeddingAsync(request.Image);

        var matchingCandidateId =
            await FindMatchingCandidateIdAsync(
                embedding);

        if (matchingCandidateId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "No matching face was found.");
        }

        return matchingCandidateId;
    }

    public async Task<ResumeAnalysis.Api.Entities.Candidate> GetCandidateAsync(
    Guid candidateId)
    {
        var candidate =
            await _context.Candidates
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == candidateId);

        if (candidate == null)
        {
            throw new UnauthorizedAccessException(
                "Candidate account was not found.");
        }

        return candidate;
    }
}