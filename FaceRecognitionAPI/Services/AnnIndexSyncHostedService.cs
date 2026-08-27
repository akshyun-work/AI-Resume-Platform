using System.Text.Json;
using FaceRecognitionAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace FaceRecognitionAPI.Services
{
    public class AnnIndexSyncHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly PythonFaceService _pythonFaceService;

        public AnnIndexSyncHostedService(
            IServiceProvider serviceProvider,
            PythonFaceService pythonFaceService)
        {
            _serviceProvider = serviceProvider;
            _pythonFaceService = pythonFaceService;
        }

        public async Task StartAsync(
            CancellationToken cancellationToken)
        {
            var annCount =
                await _pythonFaceService.GetIndexCountAsync(
                    cancellationToken);

            using var scope =
                _serviceProvider.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

            var faceEmbeddings = await context.FaceEmbeddings
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (annCount >= faceEmbeddings.Count)
            {
                return;
            }

            var candidateIds = faceEmbeddings
                .Select(f => f.CandidateId)
                .ToList();

            var embeddings = faceEmbeddings
                .Select(f =>
                    JsonSerializer.Deserialize<float[]>(
                        f.Embedding
                    )
                    ?? throw new InvalidOperationException(
                        $"Invalid embedding for CandidateId {f.CandidateId}.")
                )
                .ToList();

            await _pythonFaceService.RebuildIndexAsync(
                candidateIds,
                embeddings,
                cancellationToken);
        }

        public Task StopAsync(
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}