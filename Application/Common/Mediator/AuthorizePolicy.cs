﻿namespace Application.Common.Mediator;

[Flags]
public enum AuthorizePolicy
{
    None = 0,
    User = 1 << 0,
    Admin = 1 << 1
}