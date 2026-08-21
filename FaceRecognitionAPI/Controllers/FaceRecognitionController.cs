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
            try
            {
                await _faceRecognitionService.RegisterFaceAsync(request);

                return Ok(new
                {
                    message = "Face registered successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromForm] FaceLoginRequest request)
        {
            try
            {
                var userId = await _faceRecognitionService
                    .LoginWithFaceAsync(request);

                return Ok(new
                {
                    message = "Face verified successfully.",
                    userId
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
