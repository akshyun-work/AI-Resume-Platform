namespace FaceRecognitionAPI.Models.DTOs
{
    public class FaceRegistrationRequest
    {
        public Guid CandidateId { get; set; }

        public IFormFile Image { get; set; } = null!;
    }
}