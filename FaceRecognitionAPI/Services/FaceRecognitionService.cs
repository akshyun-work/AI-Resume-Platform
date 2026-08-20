using FaceRecognitionAPI.Models.DTOs;

namespace FaceRecognitionAPI.Services
{
    public class FaceRecognitionService
    {
        private readonly PythonFaceService _pythonFaceService;

        public FaceRecognitionService(PythonFaceService pythonFaceService)
        {
            _pythonFaceService = pythonFaceService;
        }

        public async Task<float[]> RegisterFaceAsync(FaceRegistrationRequest request)
        {
            return await _pythonFaceService.GenerateEmbeddingAsync(request.Image);
        }

        public async Task<float[]?> LoginWithFaceAsync(FaceLoginRequest request)
        {
            return await _pythonFaceService.GenerateEmbeddingAsync(request.Image);
        }
    }
}
