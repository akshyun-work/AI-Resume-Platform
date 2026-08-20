namespace FaceRecognitionAPI.Models.DTOs
{
    public class FaceRegistrationRequest
    {
        public int User_ID { get; set; }
        public IFormFile Image { get; set; } = null!;
    }
}