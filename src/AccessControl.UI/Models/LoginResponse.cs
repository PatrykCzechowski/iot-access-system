namespace AccessControl.UI.Models;

public class LoginResponse
{
    public string? AccessToken { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool? MustChangePassword { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
}
