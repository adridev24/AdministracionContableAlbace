using System.Security.Claims;

namespace BudgetControl.Api.Services
{
    public class CurrentUserService : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string UserName
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var username = user?.FindFirstValue(ClaimTypes.Name)
                    ?? user?.FindFirstValue("unique_name")
                    ?? user?.FindFirstValue("preferred_username");

                if (string.IsNullOrWhiteSpace(username))
                {
                    throw new InvalidOperationException("No se pudo identificar el usuario autenticado.");
                }

                return username;
            }
        }

        public string? UserId => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
