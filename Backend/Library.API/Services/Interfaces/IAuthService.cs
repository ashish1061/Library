namespace Library.API.Services.Interfaces;

public interface IAuthService
{
    Task<string> AuthenticateUserAsync(string username, string password);
    Task<bool> RegisterUserAsync(string username, string password, string role);
}
