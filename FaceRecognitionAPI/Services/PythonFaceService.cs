using System.Net.Http.Headers;
using FaceRecognitionAPI.Models.DTOs;

namespace FaceRecognitionAPI.Services
{
    public class PythonFaceService
    {
        private readonly HttpClient _httpClient;

        public PythonFaceService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Guid>> SearchCandidatesAsync(
            float[] embedding,
            CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                embedding
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/search-candidates",
                payload,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<AnnSearchResponse>(
                    cancellationToken: cancellationToken);

            return result?.CandidateIds ?? new List<Guid>();
        }

        public async Task RebuildIndexAsync(
            List<Guid> candidateIds,
            List<float[]> embeddings,
            CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                candidate_ids = candidateIds,
                embeddings
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/rebuild-index",
                payload,
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }

        public async Task AddEmbeddingToIndexAsync(
            Guid candidateId,
            float[] embedding,
            CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                candidate_id = candidateId,
                embedding
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/add-embedding",
                payload,
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }

        public async Task<int> GetIndexCountAsync(
            CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync(
                "/index-status",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<IndexStatusResponse>(
                    cancellationToken: cancellationToken);

            return result?.TotalEmbeddings ?? 0;
        }

        public async Task<float[]> GenerateEmbeddingAsync(
            IFormFile image,
            CancellationToken cancellationToken = default)
        {
            using var content = new MultipartFormDataContent();

            using var stream = image.OpenReadStream();
            using var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    image.ContentType ?? "application/octet-stream");

            content.Add(
                fileContent,
                "image",
                image.FileName);

            var response = await _httpClient.PostAsync(
                "/generate-embedding",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<EmbeddingResponse>(
                    cancellationToken: cancellationToken);

            if (result?.Embedding is null ||
                result.Embedding.Length == 0)
            {
                throw new InvalidOperationException(
                    "The face processing service did not return a valid embedding.");
            }

            return result.Embedding;
        }

        private class EmbeddingResponse
        {
            public float[] Embedding { get; set; } =
                Array.Empty<float>();
        }
    }
}