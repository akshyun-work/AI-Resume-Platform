namespace FaceRecognitionAPI.Models.DTOs
{
    public class FaceRegistrationRequest
    {
        public int UserId { get; set; }
        public IFormFile Image { get; set; } = null!;
    }
}