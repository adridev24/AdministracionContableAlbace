using BudgetControl.Api.DTOs;

namespace BudgetControl.Api.Services
{
    public interface IAuthService
    {
        Task<LoginResponse?> AuthenticateAsync(LoginRequest request);
    }
}
