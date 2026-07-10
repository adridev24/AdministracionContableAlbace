namespace BudgetControl.Api.Services
{
    public interface IUserContext
    {
        string UserName { get; }
        string? UserId { get; }
    }
}
