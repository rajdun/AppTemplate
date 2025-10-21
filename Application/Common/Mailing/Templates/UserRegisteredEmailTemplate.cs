namespace Application.Common.Mailing.Templates;

public class UserRegisteredEmailTemplate : EmailTemplate
{
    public string UserName { get; init; }
    public string ApplicationName { get; init; }
    
    public override string TemplateName => "UserRegistered";
    public override string Subject => $"Witamy w {ApplicationName}";
    
    public UserRegisteredEmailTemplate(string userName, string applicationName, string language = "pl")
    {
        UserName = userName;
        ApplicationName = applicationName;
        Language = language;
    }
    
    public override object[] GetParameters()
    {
        return new object[] { UserName, ApplicationName };
    }
}
