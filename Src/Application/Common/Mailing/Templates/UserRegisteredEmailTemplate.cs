using Application.Common.ValueObjects;

namespace Application.Common.Mailing.Templates;

public sealed class UserRegisteredEmailTemplate : EmailTemplate
{
    public string UserName { get; init; }
    public string ApplicationName { get; init; }

    public override string TemplateName => "UserRegistered";
    public override string Subject => Language switch
    {
        AppLanguage.En => $"Welcome to {ApplicationName}!",
        AppLanguage.Pl => $"Witamy w {ApplicationName}!",
        _ => throw new NotImplementedException("Subject not implemented for the specified language.")
    };

    public UserRegisteredEmailTemplate(string userName, string applicationName, AppLanguage language = AppLanguage.Pl)
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
