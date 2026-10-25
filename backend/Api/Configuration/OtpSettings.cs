namespace ChatApp.Api.Configuration;

public class OtpSettings
{
    public const string SectionName = "Otp";
    
    public int ExpirationMinutes { get; set; } = 5;
    public int Length { get; set; } = 6;
    public string Provider { get; set; } = "Development";
    public int MaxAttempts { get; set; } = 3;
}