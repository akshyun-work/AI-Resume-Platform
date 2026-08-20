namespace FaceRecognitionAPI.Models.DTOs
{
    public class FaceLoginRequest
    {
        public IFormFile Image { get; set; } = null!;
    }
}