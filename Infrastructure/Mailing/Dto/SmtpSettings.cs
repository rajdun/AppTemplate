namespace Infrastructure.Mailing.Dto;

public record SmtpSettings(
    string Host,
    int Port,
    string Username,
    string Password,
    string From,
    bool UseSsl
)
{
    public static string SectionName => "SmtpSettings";
};