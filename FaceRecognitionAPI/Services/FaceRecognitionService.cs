using FaceRecognitionAPI.Models.DTOs;

namespace FaceRecognitionAPI.Services
{
    public class FaceRecognitionService
    {
        public async Task RegisterFaceAsync(FaceRegistrationRequest request)
        {
            // 1. Send image to Python face-processing service
            // 2. Detect and validate face
            // 3. Generate embedding
            // 4. Search ANN index for possible duplicates
            // 5. Verify shortlisted candidates with cosine similarity
            // 6. If duplicate exists, throw/return appropriate result
            // 7. Store new embedding

            await Task.CompletedTask;
        }

        public async Task<string?> LoginWithFaceAsync(FaceLoginRequest request)
        {
            // 1. Send image to Python face-processing service
            // 2. Detect and validate face
            // 3. Generate embedding
            // 4. Search ANN index
            // 5. Verify candidates with cosine similarity
            // 6. Return matched username, or null

            await Task.CompletedTask;

            return null;
        }
    }
}
