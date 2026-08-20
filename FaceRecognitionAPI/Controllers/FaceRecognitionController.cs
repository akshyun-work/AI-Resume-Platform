using FaceRecognitionAPI.Models.DTOs;
using FaceRecognitionAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FaceRecognitionAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaceRecognitionController : ControllerBase
    {
        private readonly FaceRecognitionService _faceRecognitionService;

        public FaceRecognitionController(
            FaceRecognitionService faceRecognitionService)
        {
            _faceRecognitionService = faceRecognitionService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromForm] FaceRegistrationRequest request)
        {
            await _faceRecognitionService.RegisterFaceAsync(request);

            return Ok(new
            {
                message = "Face registered successfully."
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromForm] FaceLoginRequest request)
        {
            var username =
                await _faceRecognitionService.LoginWithFaceAsync(request);

            if (username is null)
            {
                return Unauthorized(new
                {
                    message = "Face not recognized."
                });
            }

            return Ok(new
            {
                message = "Login successful.",
                username
            });
        }
    }
}
