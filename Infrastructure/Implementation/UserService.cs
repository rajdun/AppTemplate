using Application.Common.Interfaces;
using Application.Resources;
using Application.Users.Dto;
using Application.Users.Interfaces;
using Domain.Entities.Users;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Implementation;

public class UserService(UserManager<ApplicationUser> userManager, IJwtTokenGenerator jwtTokenGenerator, IStringLocalizer<UserTranslations> localizer) 
    : IUserService
{
    public async Task<Result<TokenResult>> LoginAsync(string username, string password, CancellationToken cancellationToken = new())
    {
        var user = await userManager.FindByNameAsync(username);
        if (user == null)
        {
            return Result.Fail(localizer["UserNotFound"]);
        }
        
        var isPasswordValid = await userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            return Result.Fail(localizer["InvalidPassword"]);
        }
        
        var token = await jwtTokenGenerator.GenerateToken(user);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();
        
        return Result.Ok(new TokenResult(token, refreshToken));
    }

    public async Task<Result<TokenResult>> RegisterAsync(string username, string? email, string password, CancellationToken cancellationToken = new())
    {
        var existingUser = await userManager.FindByNameAsync(username);
        if (existingUser != null)
        {
            return Result.Fail(localizer["UsernameAlreadyExists"]);
        }
        
        var newUser = new ApplicationUser
        {
            UserName = username,
            Email = email
        };
        
        var createResult = await userManager.CreateAsync(newUser, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return Result.Fail(errors);
        }
        
        var token = await jwtTokenGenerator.GenerateToken(newUser);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();
        
        return Result.Ok(new TokenResult(token, refreshToken));
    }
}