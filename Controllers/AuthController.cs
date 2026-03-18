using BlogApiPrev.Models.DTOS;
using BlogApiPrev.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogApiPrev.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class AuthController : ControllerBase
    {
        private readonly UserServices _userServices;

        public AuthController(UserServices userServices)
        {
            _userServices = userServices;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserDTO user)
        {
            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            {
                return BadRequest(new { Success = false, Message = "Username and password are required." });
            }

            var success = await _userServices.CreateAccount(user);

            if (success)
            {
                return Ok(new { Success = true, Message = "User account created." });
            }

            return BadRequest(new { Success = false, Message = "Username is already in use." });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserDTO user)
        {
            var auth = await _userServices.Login(user);

            if (auth != null)
            {
                return Ok(auth);
            }

            return Unauthorized(new { Message = "Login was unsuccessful." });
        }
    }
}
