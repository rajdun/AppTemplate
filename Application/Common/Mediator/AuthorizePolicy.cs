namespace Application.Common.Mediator;

[Flags]
public enum AuthorizePolicy
{
    Any = 0,
    User = 1 << 0,
    Admin = 1 << 1
}