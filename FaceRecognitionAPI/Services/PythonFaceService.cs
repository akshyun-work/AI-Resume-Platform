using System.Net.Http.Headers;

namespace FaceRecognitionAPI.Services
{
    public class PythonFaceService
    {
        private readonly HttpClient _httpClient;

        public PythonFaceService(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
                await response.Content.ReadFromJsonAsync<
                    EmbeddingResponse>(cancellationToken: cancellationToken);

            if (result?.Embedding is null || result.Embedding.Length == 0)
            {
                throw new InvalidOperationException(
                    "The face processing service did not return a valid embedding.");
            }

            return result.Embedding;
        }

        private class EmbeddingResponse
        {
            public float[] Embedding { get; set; } = Array.Empty<float>();
        }
    }
}
