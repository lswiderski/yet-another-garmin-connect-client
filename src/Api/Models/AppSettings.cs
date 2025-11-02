namespace Api.Models;

public class AppSettings
{
    public GeneralSettings General { get; set; } = new();
    public AuthOptions Auth { get; set; } = new();
    public UserProfileOptions UserProfileSettings { get; set; } = new();
}

public class GeneralSettings
{
    public bool DisableSwagger { get; set; } = false;
}

public class AuthOptions
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string TokenSecret { get; set; } = string.Empty;
    public string Server { get; set; } = "global";
}

public class UserProfileOptions
{
    public int Age { get; set; } = 40;
    public int Height { get; set; } = 180;
    public int Gender { get; set; } = 1;
}