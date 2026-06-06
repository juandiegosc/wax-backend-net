using API.Controllers;
using Application.Core.Validations;
using Application.Orders.DTOs;
using Application.User.Commands;
using Application.User.DTOs;
using Application.User.Queries;
using Domain.Entities;
using Domain.Enumerators;
using Infrastructure.Email;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using UnitTests.Helpers;

namespace UnitTests.API.Controllers;

public class AccountControllerTests
{
    private readonly Mock<UserManager<User>> _userManager;
    private readonly Mock<SignInManager<User>> _signInManager;
    private readonly Mock<IEmailSender<User>> _emailSender = new();
    private readonly Mock<IMediator> _mediator;
    private readonly Mock<ILogger<AccountController>> _logger = new();
    private readonly AccountController _controller;

    private static readonly EmailSettings _emailSettings = new()
    {
        ApiToken = "test",
        FromEmail = "from@example.com",
        FromName = "WAX",
        BaseUrl = "https://app.example.com"
    };

    public AccountControllerTests()
    {
        _userManager = CreateUserManagerMock();
        _signInManager = CreateSignInManagerMock(_userManager);
        var options = Options.Create(_emailSettings);
        (_mediator, _controller) = ControllerTestFactory.Create(
            new AccountController(_signInManager.Object, _emailSender.Object, options, _logger.Object));
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<SignInManager<User>> CreateSignInManagerMock(Mock<UserManager<User>> userManagerMock)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        return new Mock<SignInManager<User>>(
            userManagerMock.Object, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);
    }

    private static User BuildUser(string email = "user@test.com") => new()
    {
        Id = Guid.NewGuid().ToString(),
        UserName = email,
        Email = email,
        EmailConfirmed = true
    };

    private static ClaimsPrincipal AuthenticatedPrincipal() =>
        new(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-id") },
            "TestAuthType"));

    private static ClaimsPrincipal UnauthenticatedPrincipal() =>
        new(new ClaimsIdentity());

    // ── RegisterUser ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterUser_WhenSucceeds_AddsEnrolledRoleAndReturnsOk()
    {
        var dto = new RegisterDto { Email = "new@test.com", Password = "Pass123!" };
        _userManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManager
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), Roles.Enrolled))
            .ReturnsAsync(IdentityResult.Success);
        _userManager
            .Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("confirmation-token");

        var result = await _controller.RegisterUser(dto);

        result.Should().BeOfType<OkResult>();
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), Roles.Enrolled), Times.Once);
    }

    [Fact]
    public async Task RegisterUser_WhenIdentityFails_ReturnsValidationProblem()
    {
        var dto = new RegisterDto { Email = "bad@test.com", Password = "weak" };
        _userManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), dto.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort", Description = "Password too short" }));

        var result = await _controller.RegisterUser(dto);

        result.Should().BeAssignableTo<ObjectResult>()
            .Which.StatusCode.Should().Be(400);
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    // ── NEW: 6.1 confirmation email tests ─────────────────────────────────────

    [Fact]
    public async Task RegisterUser_WhenSucceeds_SendsConfirmationEmailWithEncodedToken()
    {
        var dto = new RegisterDto { Email = "new@test.com", Password = "Pass123!" };
        const string rawToken = "token+with/special=chars";
        var expectedEncodedToken = Uri.EscapeDataString(rawToken);

        _userManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManager
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), Roles.Enrolled))
            .ReturnsAsync(IdentityResult.Success);
        _userManager
            .Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync(rawToken);

        string? capturedLink = null;
        _emailSender
            .Setup(e => e.SendConfirmationLinkAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<User, string, string>((u, email, link) => capturedLink = link)
            .Returns(Task.CompletedTask);

        var result = await _controller.RegisterUser(dto);

        result.Should().BeOfType<OkResult>();
        _emailSender.Verify(
            e => e.SendConfirmationLinkAsync(It.IsAny<User>(), dto.Email, It.IsAny<string>()),
            Times.Once);
        capturedLink.Should().Contain(expectedEncodedToken);
        capturedLink.Should().NotContain(rawToken.Replace("+", "").Replace("/", "").Replace("=", ""));
    }

    [Fact]
    public async Task RegisterUser_WhenEmailSenderThrows_StillReturnsOkAndLogsError()
    {
        var dto = new RegisterDto { Email = "new@test.com", Password = "Pass123!" };

        _userManager
            .Setup(m => m.CreateAsync(It.IsAny<User>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManager
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), Roles.Enrolled))
            .ReturnsAsync(IdentityResult.Success);
        _userManager
            .Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("some-token");

        _emailSender
            .Setup(e => e.SendConfirmationLinkAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email failure"));

        var result = await _controller.RegisterUser(dto);

        result.Should().BeOfType<OkResult>();

        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send confirmation email")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── GetUserInfo ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserInfo_WhenNotAuthenticated_ReturnsNoContent()
    {
        _controller.ControllerContext.HttpContext.User = UnauthenticatedPrincipal();

        var result = await _controller.GetUserInfo();

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetUserInfo_WhenUserNotFound_ReturnsUnauthorized()
    {
        _controller.ControllerContext.HttpContext.User = AuthenticatedPrincipal();
        _userManager
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((User?)null);

        var result = await _controller.GetUserInfo();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetUserInfo_WhenAuthenticated_ReturnsUserInfoWithRoles()
    {
        var user = BuildUser("me@test.com");
        _controller.ControllerContext.HttpContext.User = AuthenticatedPrincipal();
        _userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([Roles.Registered]);

        var result = await _controller.GetUserInfo();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
        var value = ok.Value!;
        var type = value.GetType();
        type.GetProperty("Email")!.GetValue(value).Should().Be(user.Email);
        type.GetProperty("UserName")!.GetValue(value).Should().Be(user.UserName);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_CallsSignOutAndReturnsNoContent()
    {
        _signInManager.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

        var result = await _controller.Logout();

        result.Should().BeOfType<NoContentResult>();
        _signInManager.Verify(s => s.SignOutAsync(), Times.Once);
    }

    // ── CreateOrUpdateBillingAddress ──────────────────────────────────────────

    [Fact]
    public async Task CreateOrUpdateBillingAddress_DelegatesToMediator()
    {
        var billingInfo = BuildBillingInfoRequest();
        _mediator
            .Setup(m => m.Send(It.IsAny<CreateOrUpdateBillingAddressCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BillingAddressDto>.Success(BuildBillingAddressDto()));

        await _controller.CreateOrUpdateBillingAddress(billingInfo);

        _mediator.Verify(
            m => m.Send(
                It.Is<CreateOrUpdateBillingAddressCommand>(c => c.BillingInfo == billingInfo),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── GetSavedAddress ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSavedAddress_DelegatesToMediator()
    {
        var dto = BuildBillingAddressDto();
        _mediator
            .Setup(m => m.Send(It.IsAny<GetBillingAddressQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BillingAddressDto>.Success(dto));

        await _controller.GetSavedAddress();

        _mediator.Verify(
            m => m.Send(It.IsAny<GetBillingAddressQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── ForgotPassword ────────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_WhenUserNotFound_ReturnsOkWithoutSendingEmail()
    {
        var req = new ForgotPasswordRequest { Email = "ghost@test.com" };
        _userManager.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync((User?)null);

        var result = await _controller.ForgotPassword(req);

        result.Should().BeOfType<OkResult>();
        _emailSender.Verify(
            e => e.SendPasswordResetCodeAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_WhenUserFound_GeneratesTokenAndSendsEmail()
    {
        var user = BuildUser("found@test.com");
        const string resetCode = "reset-token-abc";
        var req = new ForgotPasswordRequest { Email = user.Email! };

        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager
            .Setup(m => m.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetCode);
        _emailSender
            .Setup(e => e.SendPasswordResetCodeAsync(user, user.Email!, resetCode))
            .Returns(Task.CompletedTask);

        var result = await _controller.ForgotPassword(req);

        result.Should().BeOfType<OkResult>();
        _userManager.Verify(m => m.GeneratePasswordResetTokenAsync(user), Times.Once);
        _emailSender.Verify(e => e.SendPasswordResetCodeAsync(user, user.Email!, resetCode), Times.Once);
    }

    // ── ResetPassword ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_WhenUserNotFound_ReturnsOk()
    {
        var req = new ResetPasswordRequest { Email = "ghost@test.com", Code = "token", NewPassword = "Pass123!" };
        _userManager.Setup(m => m.FindByEmailAsync(req.Email)).ReturnsAsync((User?)null);

        var result = await _controller.ResetPassword(req);

        result.Should().BeOfType<OkResult>();
        _userManager.Verify(
            m => m.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPassword_WhenSucceeds_ReturnsOk()
    {
        var user = BuildUser("reset@test.com");
        var req = new ResetPasswordRequest { Email = user.Email!, Code = "valid-token", NewPassword = "NewPass123!" };

        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager
            .Setup(m => m.ResetPasswordAsync(user, req.Code, req.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.ResetPassword(req);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ResetPassword_WhenFails_ReturnsValidationProblem()
    {
        var user = BuildUser("reset@test.com");
        var req = new ResetPasswordRequest { Email = user.Email!, Code = "bad-token", NewPassword = "weak" };

        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager
            .Setup(m => m.ResetPasswordAsync(user, req.Code, req.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "InvalidToken", Description = "Invalid token" }));

        var result = await _controller.ResetPassword(req);

        result.Should().BeAssignableTo<ObjectResult>()
            .Which.StatusCode.Should().Be(400);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CreateOrUpdateBillingInfoRequest BuildBillingInfoRequest() => new()
    {
        Name = "Jane Doe",
        Line1 = "Street 123",
        City = "Quito",
        State = "Pichincha",
        PostalCode = "170101",
        Country = "EC",
        IdentificationNumber = "1723456789",
        IdentificationType = "CI",
        FirstName = "Jane",
        LastName = "Doe",
        Phone = "+593999999999"
    };

    private static BillingAddressDto BuildBillingAddressDto() => new()
    {
        Name = "Jane Doe",
        Line1 = "Street 123",
        City = "Quito",
        State = "Pichincha",
        PostalCode = "170101",
        Country = "EC"
    };
}
