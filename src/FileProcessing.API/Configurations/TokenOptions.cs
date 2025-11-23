namespace FileProcessing.API.Configurations;

public class TokenOptions
{
    public string Key { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpiresMinutes { get; set; }
}
