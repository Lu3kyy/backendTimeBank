using System.Security.Claims;
using BlogApiPrev.Models.DTOS;
using BlogApiPrev.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogApiPrev.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HelpController : ControllerBase
    {
        private readonly UserServices _userServices;
        private readonly ILogger<HelpController> _logger;
        private readonly IWebHostEnvironment _environment;

        public HelpController(UserServices userServices, ILogger<HelpController> logger, IWebHostEnvironment environment)
        {
            _userServices = userServices;
            _logger = logger;
            _environment = environment;
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
            var traceId = Request.Headers.TryGetValue("x-proxy-trace-id", out var tid) ? tid.ToString() : "(none)";

            if (string.IsNullOrWhiteSpace(post.Category) || string.IsNullOrWhiteSpace(post.PostType) || string.IsNullOrWhiteSpace(post.Title) || string.IsNullOrWhiteSpace(post.Description))
            {
                return BadRequest(new { Message = "Category, post type, title, and description are required." });
            }

            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                _logger.LogWarning("[trace:{TraceId}] CreateHelpPost: auth claim missing. Claims: {Claims}",
                    traceId, string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            try
            {
                var created = await _userServices.CreateHelpPostAsync(userId.Value, post);
                if (created == null)
                {
                    _logger.LogWarning("[trace:{TraceId}] CreateHelpPost: service returned null for userId={UserId} category={Category} postType={PostType}",
                        traceId, userId.Value, post.Category, post.PostType);
                    return BadRequest(new { Message = "Invalid category or post type." });
                }

                return Ok(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[trace:{TraceId}] CreateHelpPost threw for userId={UserId}", traceId, userId.Value);

                if (_environment.IsDevelopment())
                {
                    return StatusCode(500, new
                    {
                        Message = "An unexpected error occurred creating the help post.",
                        TraceId = traceId,
                        ErrorType = ex.GetType().FullName,
                        Error = ex.Message,
                        InnerError = ex.InnerException?.Message
                    });
                }

                return StatusCode(500, new { Message = "An unexpected error occurred creating the help post.", TraceId = traceId });
            }
        }

        [HttpGet("help-posts")]
        [Authorize]
        public async Task<IActionResult> GetHelpPosts(
            [FromQuery] string? category = null,
            [FromQuery] string? postType = null,
            [FromQuery] double? latitude = null,
            [FromQuery] double? longitude = null,
            [FromQuery] double? radiusKm = null)
        {
            var posts = await _userServices.GetHelpPostsAsync(category, postType, latitude, longitude, radiusKm);
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

        [HttpGet("help-posts/{postId}")]
        [Authorize]
        public async Task<IActionResult> GetHelpPost(int postId)
        {
            var post = await _userServices.GetHelpPostByIdAsync(postId);
            if (post == null)
            {
                return NotFound(new { Message = "Help post not found." });
            }

            return Ok(post);
        }

        [HttpPut("help-posts/{postId}")]
        [Authorize]
        public async Task<IActionResult> UpdateHelpPost(int postId, [FromBody] HelpPostUpdateDTO updates)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var updated = await _userServices.UpdateHelpPostAsync(userId.Value, postId, updates);
            if (updated == null)
            {
                return NotFound(new { Message = "Help post not found or not allowed." });
            }

            return Ok(updated);
        }

        [HttpDelete("help-posts/{postId}")]
        [Authorize]
        public async Task<IActionResult> DeleteHelpPost(int postId)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var success = await _userServices.DeleteHelpPostAsync(userId.Value, postId);
            if (!success)
            {
                return NotFound(new { Message = "Help post not found or not allowed." });
            }

            return Ok(new { Success = true, Message = "Help post deleted." });
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
