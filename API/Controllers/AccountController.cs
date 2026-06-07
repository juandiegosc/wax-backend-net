using Application.User.Commands;
using Application.User.DTOs;
using Application.User.Queries;
using Domain.Entities;
using Domain.Enumerators;
using Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

public class AccountController(
    SignInManager<User> signInManager,
    IEmailSender<User> emailSender,
    IOptions<EmailSettings> emailSettings,
    ILogger<AccountController> logger)
    : BaseApiController
{
    private readonly EmailSettings _emailSettings = emailSettings.Value;

    [HttpPost("register")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> RegisterUser(RegisterDto registerDto)
    {
        var user = new User { UserName = registerDto.Email, Email = registerDto.Email };

        var result = await signInManager.UserManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem();
        }

        await signInManager.UserManager.AddToRoleAsync(user, Roles.Enrolled);

        try
        {
            var token = await signInManager.UserManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var link = $"{_emailSettings.BaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";
            await emailSender.SendConfirmationLinkAsync(user, registerDto.Email, link);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send confirmation email to {Email}", registerDto.Email);
        }

        return Ok();
    }

    [HttpGet("user-info")]
    [ProducesResponseType(200)]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> GetUserInfo()
    {
        if (User.Identity?.IsAuthenticated == false) return NoContent();

        var user = await signInManager.UserManager.GetUserAsync(User);

        if (user == null) return Unauthorized();

        var roles = await signInManager.UserManager.GetRolesAsync(user);

        return Ok(new
        {
            user.Email,
            user.UserName,
            Roles = roles
        });
    }

    [HttpPost("logout")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        return NoContent();
    }

    [HttpPost("billing-address")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult> CreateOrUpdateBillingAddress(CreateOrUpdateBillingInfoRequest billingInfo)
    {
        return await HandleCommand(new CreateOrUpdateBillingAddressCommand { BillingInfo = billingInfo });
    }

    [HttpGet("billing-address")]
    [ProducesResponseType(typeof(BillingAddress), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<BillingAddress>> GetSavedAddress()
    {
        return await HandleQuery(new GetBillingAddressQuery());
    }

    [Authorize(Roles = Roles.Registered)]
    [HttpPost("forgot-password")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> ForgotPassword(ForgotPasswordRequest forgotPasswordRequest)
    {
        var user = await signInManager.UserManager.FindByEmailAsync(forgotPasswordRequest.Email);

        if (user == null)
            return Ok();

        var code = await signInManager.UserManager.GeneratePasswordResetTokenAsync(user);

        await emailSender.SendPasswordResetCodeAsync(user, forgotPasswordRequest.Email, code);

        return Ok();
    }

    [Authorize(Roles = Roles.Registered)]
    [HttpPost("reset-password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> ResetPassword(ResetPasswordRequest passwordRequest)
    {
        var user = await signInManager.UserManager.FindByEmailAsync(passwordRequest.Email);

        if (user == null)
            return Ok();

        var result = await signInManager.UserManager.ResetPasswordAsync(
            user,
            passwordRequest.Code,
            passwordRequest.NewPassword);

        if (result.Succeeded) return Ok();
        foreach (var error in result.Errors)
            ModelState.AddModelError(error.Code, error.Description);
        return ValidationProblem();
    }
}
