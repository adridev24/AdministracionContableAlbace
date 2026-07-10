using Microsoft.AspNetCore.Mvc;
using BudgetControl.Api.DTOs;
using BudgetControl.Api.Services;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.AuthenticateAsync(request);
            if (result == null) return Unauthorized(new { message = "Invalid credentials" });
            return Ok(result);
        }
    }
}
