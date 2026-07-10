namespace BudgetControl.Api.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = null!;
        public DateTime Expires { get; set; }
        public string Username { get; set; } = null!;
    }
}
