using System.Security.Claims;
using BlogApiPrev.Models.DTOS;
using BlogApiPrev.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogApiPrev.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DmsController : ControllerBase
    {
        private readonly UserServices _userServices;

        public DmsController(UserServices userServices)
        {
            _userServices = userServices;
        }

        [HttpGet("inbox")]
        public async Task<IActionResult> GetInbox()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var inbox = await _userServices.GetDirectMessageInboxAsync(userId.Value);
            return Ok(inbox);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var unreadCount = await _userServices.GetDirectMessageUnreadCountAsync(userId.Value);
            return Ok(new { unreadCount });
        }

        [HttpGet("conversations/{otherUsername}/messages")]
        public async Task<IActionResult> GetConversation(string otherUsername)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var messages = await _userServices.GetDirectMessageConversationAsync(userId.Value, otherUsername);
            if (messages == null)
            {
                return NotFound(new { Message = "Conversation not found." });
            }

            return Ok(messages);
        }

        [HttpPost("conversations/{otherUsername}/messages")]
        public async Task<IActionResult> SendDirectMessage(string otherUsername, [FromBody] SendChatMessageDTO input)
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

            var message = await _userServices.SendDirectMessageAsync(userId.Value, otherUsername, input.Message);
            if (message == null)
            {
                return BadRequest(new { Message = "Unable to send direct message." });
            }

            return Ok(message);
        }

        [HttpPost("conversations/{otherUsername}/read")]
        public async Task<IActionResult> MarkConversationRead(string otherUsername)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
            {
                return Unauthorized(new { Message = "Token is missing user id." });
            }

            var markedCount = await _userServices.MarkDirectMessageConversationReadAsync(userId.Value, otherUsername);
            return Ok(new { markedCount });
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