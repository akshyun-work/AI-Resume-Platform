using FaceRecognitionAPI.Data;
using FaceRecognitionAPI.Models.DTOs;
using FaceRecognitionAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

    private async Task<int> FindMatchingUserIdAsync(float[] embedding)
    {
        const double similarityThreshold = 0.75;

        var candidateUserIds =
            await _pythonFaceService.SearchCandidatesAsync(embedding);
        Console.WriteLine(
            $"Candidate IDs: {string.Join(", ", candidateUserIds)}"
            );
        if (candidateUserIds.Count == 0)
        {
            return 0;
        }

        var candidateEmbeddings = await _context.FaceEmbeddings
            .AsNoTracking()
            .Where(f => candidateUserIds.Contains(f.UserId))
            .ToListAsync();

        int bestMatchingUserId = 0;
        double highestSimilarity = double.MinValue;

        foreach (var storedFace in candidateEmbeddings)
        {
            var storedEmbedding =
                JsonSerializer.Deserialize<float[]>(storedFace.Embedding);

            if (storedEmbedding == null ||
                storedEmbedding.Length != embedding.Length)
            {
                continue;
            }

            double similarity =
                CalculateCosineSimilarity(embedding, storedEmbedding);

            Console.WriteLine(
                $"Candidate UserId: {storedFace.UserId}, " +
                $"Similarity: {similarity}"
            );

            if (similarity > highestSimilarity)
            {
                highestSimilarity = similarity;
                bestMatchingUserId = storedFace.UserId;
            }
        }

        return highestSimilarity >= similarityThreshold
            ? bestMatchingUserId
            : 0;
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
            dotProduct += firstEmbedding[i] * secondEmbedding[i];
            firstMagnitude += firstEmbedding[i] * firstEmbedding[i];
            secondMagnitude += secondEmbedding[i] * secondEmbedding[i];
        }

        if (firstMagnitude == 0 || secondMagnitude == 0)
        {
            return 0;
        }

        return dotProduct /
               (Math.Sqrt(firstMagnitude) * Math.Sqrt(secondMagnitude));
    }

    public async Task RegisterFaceAsync(FaceRegistrationRequest request)
    {
        var embedding = await _pythonFaceService
            .GenerateEmbeddingAsync(request.Image);

        var existingRegistration = await _context.FaceEmbeddings
            .AnyAsync(f => f.UserId == request.UserId);

        if (existingRegistration)
        {
            throw new InvalidOperationException(
                "This user already has a registered face."
            );
        }

        var matchingUserId = await FindMatchingUserIdAsync(embedding);

        if (matchingUserId != 0)
        {
            throw new InvalidOperationException(
                "This face is already linked to another account."
            );
        }

        var faceEmbedding = new FaceEmbedding
        {
            UserId = request.UserId,
            Embedding = JsonSerializer.Serialize(embedding)
        };

        _context.FaceEmbeddings.Add(faceEmbedding);

        await _context.SaveChangesAsync();

        await _pythonFaceService.AddEmbeddingToIndexAsync
        (
            request.UserId,
            embedding
        );
    }

    public async Task<int> LoginWithFaceAsync(FaceLoginRequest request)
    {
        var embedding = await _pythonFaceService
            .GenerateEmbeddingAsync(request.Image);

        var matchingUserId = await FindMatchingUserIdAsync(embedding);

        if (matchingUserId == 0)
        {
            throw new InvalidOperationException(
                "No matching face was found."
            );
        }

        return matchingUserId;
    }
}