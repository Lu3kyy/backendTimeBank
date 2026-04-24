using System.Security.Claims;
using Azure;
using BlogApiPrev.Models.DTOS;
using BlogApiPrev.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogApiPrev.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly UserServices _userServices;
        private readonly BlobStorageService _blobStorageService;

        public ProfileController(UserServices userServices, BlobStorageService blobStorageService)
        {
            _userServices = userServices;
            _blobStorageService = blobStorageService;
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

        [HttpGet("profiles")]
        public async Task<IActionResult> GetProfiles(
            [FromQuery] string? search = null,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20,
            [FromQuery] bool random = false,
            [FromQuery] bool onlyComplete = false,
            [FromQuery] string? city = null,
            [FromQuery] double? latitude = null,
            [FromQuery] double? longitude = null,
            [FromQuery] double? radiusKm = null)
        {
            var profiles = await _userServices.GetProfilesAsync(search, skip, take, random, onlyComplete, city, latitude, longitude, radiusKm);
            return Ok(profiles);
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

        // [HttpPost("profile-picture")]
        // [RequestSizeLimit(10_000_000)]
        // public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile file)
        // {
        //     if (!_blobStorageService.IsConfigured())
        //     {
        //         return StatusCode(StatusCodes.Status503ServiceUnavailable, new
        //         {
        //             Message = "Blob storage is not configured. Set BlobStorage:ConnectionString and BlobStorage:ContainerName in app settings."
        //         });
        //     }

        //     if (file == null || file.Length == 0)
        //     {
        //         return BadRequest(new { Message = "An image file is required." });
        //     }

        //     var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        //     if (!allowedTypes.Contains(file.ContentType))
        //     {
        //         return BadRequest(new { Message = "Only jpeg, png, or webp images are allowed." });
        //     }

        //     var userId = GetUserIdFromClaims();
        //     if (userId == null)
        //     {
        //         return Unauthorized(new { Message = "Token is missing user id." });
        //     }

        //     try
        //     {
        //         var blobUrl = await _blobStorageService.UploadProfileImageAsync(file, userId.Value);
        //         var updatedProfile = await _userServices.SetProfilePictureUrlAsync(userId.Value, blobUrl);
        //         if (updatedProfile == null)
        //         {
        //             return NotFound(new { Message = "User profile not found." });
        //         }

        //         return Ok(updatedProfile);
        //     }
        //     catch (RequestFailedException ex)
        //     {
        //         return StatusCode(StatusCodes.Status502BadGateway, new
        //         {
        //             Message = "Failed to upload image to blob storage.",
        //             Details = ex.Message
        //         });
        //     }
        // }

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
