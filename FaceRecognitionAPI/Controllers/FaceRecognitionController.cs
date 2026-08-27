using System.Security.Claims;
using FaceRecognitionAPI.Models.DTOs;
using FaceRecognitionAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAnalysis.Api.Services.Interfaces;

namespace FaceRecognitionAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaceRecognitionController : ControllerBase
    {
        private readonly FaceRecognitionService _faceRecognitionService;
        private readonly IJwtTokenService _jwtTokenService;

        public FaceRecognitionController(
            FaceRecognitionService faceRecognitionService,
            IJwtTokenService jwtTokenService)
        {
            _faceRecognitionService = faceRecognitionService;
            _jwtTokenService = jwtTokenService;
        }

        // ============================================================
        // REGISTER FACE
        // Authenticated profile operation
        // ============================================================

        [Authorize]
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromForm] FaceRegistrationRequest request)
        {
            try
            {
                // Get the candidate ID from the authenticated JWT.
                var candidateIdClaim =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!Guid.TryParse(
                        candidateIdClaim,
                        out var candidateId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid candidate identity."
                    });
                }

                // Never trust CandidateId supplied by the frontend.
                request.CandidateId = candidateId;

                await _faceRecognitionService.RegisterFaceAsync(
                    request);

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

        // ============================================================
        // FACE LOGIN
        // Public authentication operation
        // No [Authorize] here because the user does not have a JWT yet.
        // ============================================================

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromForm] FaceLoginRequest request)
        {
            try
            {
                var candidateId =
                    await _faceRecognitionService
                        .LoginWithFaceAsync(request);

                var candidate =
                    await _faceRecognitionService
                        .GetCandidateAsync(candidateId);

                var (token, expiresAt) =
                    _jwtTokenService.GenerateToken(candidate);

                return Ok(new
                {
                    message = "Face verified successfully.",
                    token,
                    expiresAt,
                    candidate = new
                    {
                        candidate.Id,
                        candidate.Email,
                        candidate.FullName,
                        candidate.Phone,
                        candidate.CreatedAt
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
        }
    }
}