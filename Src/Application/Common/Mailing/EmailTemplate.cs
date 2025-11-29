using Application.Common.ValueObjects;

namespace Application.Common.Mailing;

public abstract class EmailTemplate
{
    public abstract string TemplateName { get; }
    public virtual AppLanguage Language { get; init; } = AppLanguage.Pl;
    public abstract string Subject { get; }
    public abstract object[] GetParameters();

    public ICollection<EmailAttachment> Attachments { get; init; } = [];
    public string? ReplyTo { get; init; }
    public ICollection<string> Cc { get; init; } = [];
    public ICollection<string> Bcc { get; init; } = [];
}

#pragma warning disable CA1819
public record EmailAttachment(string FileName, byte[] Content, string ContentType);
#pragma warning restore CA1819
