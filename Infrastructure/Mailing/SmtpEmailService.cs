using Application.Common.Mailing;
using Infrastructure.Mailing.Dto;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Infrastructure.Mailing;

public class SmtpEmailService(SmtpSettings settings)
    : IEmailService
{
    
}