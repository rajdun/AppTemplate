namespace Infrastructure.Mailing.Dto;

public class SmtpSettings
{
    public static string SectionName => "SmtpSettings";
    public string Host { get; init; }
    public int Port { get; init; }
    public string Username { get; init; }
    public string Password { get; init; }
    public string From { get; init; }
    public bool UseSsl { get; init; }


    public SmtpSettings(string host, int port, string username, string password, string from, bool useSsl)
    {
        Host = host;
        Port = port;
        Username = username;
        Password = password;
        From = from;
        UseSsl = useSsl;
    }

    public SmtpSettings()
    {
        Host = "";
        Username = "";
        Password = "";
        From = "";
    }
};