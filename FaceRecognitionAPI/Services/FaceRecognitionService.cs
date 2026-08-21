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