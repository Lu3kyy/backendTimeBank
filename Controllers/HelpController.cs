using System.Security.Claims;
using BlogApiPrev.Models.DTOS;
using BlogApiPrev.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogApiPrev.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class HelpController : ControllerBase
    {
        private readonly UserServices _userServices;

        public HelpController(UserServices userServices)
        {
            _userServices = userServices;
        }

        [HttpGet("help-categories")]
        [AllowAnonymous]
        public IActionResult GetHelpCategories()
        {
            return Ok(_userServices.GetHelpCategories());
        }

        [HttpPost("help-posts")]
        [Authorize]
        public async Task<IActionResult> CreateHelpPost([FromBody] HelpPostCreateDTO post)
        {
            if (string.IsNullOrWhiteSpace(post.Category) || string.IsNullOrWhiteSpace(post.Subcategory) || string.IsNullOrWhiteSpace(post.Title) || string.IsNullOrWhiteSpace(post.Description))
            {
                return BadRequest(new { Message = "Category, subcategory, title, and description are required." });
            }

            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var created = await _userServices.CreateHelpPostAsync(userId.Value, post);
            if (created == null)
            {
                return BadRequest(new { Message = "Unable to create help post." });
            }

            return Ok(created);
        }

        [HttpGet("help-posts")]
        [Authorize]
        public async Task<IActionResult> GetHelpPosts(
            [FromQuery] string? category = null,
            [FromQuery] string? subcategory = null,
            [FromQuery] double? latitude = null,
            [FromQuery] double? longitude = null,
            [FromQuery] double? radiusKm = null)
        {
            var posts = await _userServices.GetHelpPostsAsync(category, subcategory, latitude, longitude, radiusKm);
            return Ok(posts);
        }

        [HttpGet("my-help-posts")]
        [Authorize]
        public async Task<IActionResult> GetMyHelpPosts()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var posts = await _userServices.GetMyHelpPostsAsync(userId.Value);
            return Ok(posts);
        }

        [HttpPost("help-posts/{postId}/close")]
        [Authorize]
        public async Task<IActionResult> CloseHelpPost(int postId)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var success = await _userServices.CloseHelpPostAsync(userId.Value, postId);
            if (!success)
            {
                return NotFound(new { Message = "Help post not found or not allowed." });
            }

            return Ok(new { Success = true });
        }

        [HttpPost("chats/start")]
        [Authorize]
        public async Task<IActionResult> StartChat([FromBody] StartChatDTO input)
        {
            if (input.HelpPostId <= 0)
            {
                return BadRequest(new { Message = "Help post id is required." });
            }

            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var thread = await _userServices.StartChatAsync(userId.Value, input);
            if (thread == null)
            {
                return BadRequest(new { Message = "Unable to start chat for this post." });
            }

            return Ok(thread);
        }

        [HttpGet("chats")]
        [Authorize]
        public async Task<IActionResult> GetMyChats()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var chats = await _userServices.GetMyChatsAsync(userId.Value);
            return Ok(chats);
        }

        [HttpGet("chats/{chatId}/messages")]
        [Authorize]
        public async Task<IActionResult> GetChatMessages(int chatId)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var messages = await _userServices.GetChatMessagesAsync(userId.Value, chatId);
            if (messages == null)
            {
                return NotFound(new { Message = "Chat not found or no access." });
            }

            return Ok(messages);
        }

        [HttpPost("chats/{chatId}/messages")]
        [Authorize]
        public async Task<IActionResult> SendMessage(int chatId, [FromBody] SendChatMessageDTO input)
        {
            if (string.IsNullOrWhiteSpace(input.Message))
            {
                return BadRequest(new { Message = "Message is required." });
            }

            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var message = await _userServices.SendChatMessageAsync(userId.Value, chatId, input);
            if (message == null)
            {
                return BadRequest(new { Message = "Unable to send message." });
            }

            return Ok(message);
        }

        [HttpPost("chats/{chatId}/end")]
        [Authorize]
        public async Task<IActionResult> EndChat(int chatId)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var success = await _userServices.EndChatAsync(userId.Value, chatId);
            if (!success)
            {
                return NotFound(new { Message = "Chat not found or no access." });
            }

            return Ok(new { Success = true, Message = "Chat ended." });
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
