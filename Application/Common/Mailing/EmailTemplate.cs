﻿namespace Application.Common.Mailing;

public abstract class EmailTemplate
{
    public abstract string TemplateName { get; }
    public virtual string Language { get; init; } = "pl";
    public abstract string Subject { get; }
    public abstract object[] GetParameters();
    
    public List<EmailAttachment> Attachments { get; init; } = new();
    public string? ReplyTo { get; init; }
    public List<string> Cc { get; init; } = new();
    public List<string> Bcc { get; init; } = new();
}

public record EmailAttachment(string FileName, byte[] Content, string ContentType);
