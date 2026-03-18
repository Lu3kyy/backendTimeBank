using System.Security.Claims;
using BlogApiPrev.Models.DTOS;
using BlogApiPrev.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogApiPrev.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly UserServices _userServices;

        public ProfileController(UserServices userServices)
        {
            _userServices = userServices;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var profile = await _userServices.GetUserProfileByIdAsync(userId.Value);
            if (profile == null)
            {
                return NotFound(new { Message = "User profile not found." });
            }

            return Ok(profile);
        }

        [HttpPost("profile")]
        public async Task<IActionResult> CreateOrStoreProfile([FromBody] ProfileUpsertDTO profile)
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                return BadRequest(new { Message = "Name is required to create profile." });
            }

            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var result = await _userServices.CreateProfileAsync(userId.Value, profile);
            if (result == null)
            {
                return NotFound(new { Message = "User account not found." });
            }

            return Ok(result);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> EditProfile([FromBody] ProfileUpdateDTO profile)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var result = await _userServices.UpdateProfileAsync(userId.Value, profile);
            if (result == null)
            {
                return NotFound(new { Message = "User profile not found." });
            }

            return Ok(result);
        }

        private int? GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return userId;
        }
    }
}
