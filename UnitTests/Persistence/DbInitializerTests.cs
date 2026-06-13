using Domain.Entities;
using Domain.Enumerators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Persistence;

namespace UnitTests.Persistence;

public class DbInitializerTests
{
    private readonly Mock<UserManager<User>> _userManager;
    private readonly Mock<RoleManager<IdentityRole>> _roleManager;
    private readonly Mock<IConfiguration> _configuration;
    private readonly Mock<ILogger<DbInitializer>> _logger;

    public DbInitializerTests()
    {
        var userStore = new Mock<IUserStore<User>>();
        _userManager = new Mock<UserManager<User>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _roleManager = new Mock<RoleManager<IdentityRole>>(
            roleStore.Object, null!, null!, null!, null!);

        _configuration = new Mock<IConfiguration>();
        _logger = new Mock<ILogger<DbInitializer>>();
    }

    private DbInitializer CreateInitializer()
    {
        return CreateInitializerWithContext().initializer;
    }

    private (DbInitializer initializer, WriteDbContext context) CreateInitializerWithContext()
    {
        var options = new DbContextOptionsBuilder<WriteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new WriteDbContext(options);

        var initializer = new DbInitializer(
            _userManager.Object,
            _roleManager.Object,
            _configuration.Object,
            context,
            _logger.Object);

        return (initializer, context);
    }

    private void SetupSkipAdminAndRoles()
    {
        _roleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        _configuration.Setup(c => c["Admin:Email"]).Returns((string?)null);
        _configuration.Setup(c => c["Admin:Password"]).Returns((string?)null);
    }

    [Fact]
    public async Task InitializeAsync_WhenRolesDoNotExist_CreatesAllRoles()
    {
        _roleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _roleManager.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(IdentityResult.Success);
        _configuration.Setup(c => c["Admin:Email"]).Returns((string?)null);
        _configuration.Setup(c => c["Admin:Password"]).Returns((string?)null);

        await CreateInitializer().InitializeAsync();

        _roleManager.Verify(r => r.CreateAsync(It.IsAny<IdentityRole>()), Times.Exactly(Roles.All.Count));
    }

    [Fact]
    public async Task InitializeAsync_WhenRolesExist_SkipsCreation()
    {
        _roleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        _configuration.Setup(c => c["Admin:Email"]).Returns((string?)null);
        _configuration.Setup(c => c["Admin:Password"]).Returns((string?)null);

        await CreateInitializer().InitializeAsync();

        _roleManager.Verify(r => r.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_WhenBothAdminConfigsEmpty_SkipsAdminSeed()
    {
        _roleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        _configuration.Setup(c => c["Admin:Email"]).Returns((string?)null);
        _configuration.Setup(c => c["Admin:Password"]).Returns((string?)null);

        await CreateInitializer().InitializeAsync();

        _userManager.Verify(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_WhenAdminConfigProvided_CreatesAdminWithCorrectEmail()
    {
        _roleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        _configuration.Setup(c => c["Admin:Email"]).Returns("admin@example.com");
        _configuration.Setup(c => c["Admin:Password"]).Returns("Admin123!");
        _userManager.Setup(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(u => u.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        await CreateInitializer().InitializeAsync();

        _userManager.Verify(u => u.CreateAsync(
            It.Is<User>(user => user.Email == "admin@example.com"),
            "Admin123!"), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WhenAdminConfigProvided_AssignsAdminRole()
    {
        _roleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        _configuration.Setup(c => c["Admin:Email"]).Returns("admin@example.com");
        _configuration.Setup(c => c["Admin:Password"]).Returns("Admin123!");
        _userManager.Setup(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(u => u.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        await CreateInitializer().InitializeAsync();

        _userManager.Verify(u => u.AddToRoleAsync(It.IsAny<User>(), Roles.Admin), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WhenRoleCreationFails_LogsError()
    {
        _roleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _roleManager.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Failed" }));
        _configuration.Setup(c => c["Admin:Email"]).Returns((string?)null);
        _configuration.Setup(c => c["Admin:Password"]).Returns((string?)null);

        await CreateInitializer().InitializeAsync();

        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InitializeAsync_WhenAdminCreationFails_LogsError()
    {
        _roleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        _configuration.Setup(c => c["Admin:Email"]).Returns("admin@example.com");
        _configuration.Setup(c => c["Admin:Password"]).Returns("Admin123!");
        _userManager.Setup(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email taken" }));
        _userManager.Setup(u => u.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        await CreateInitializer().InitializeAsync();

        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InitializeAsync_WhenQuotationRulesTableEmpty_SeedsFourDefaultRules()
    {
        SetupSkipAdminAndRoles();
        var (initializer, context) = CreateInitializerWithContext();

        await initializer.InitializeAsync();

        var rules = await context.QuotationRules.ToListAsync();
        rules.Should().HaveCount(4);
        rules.Should().OnlyContain(r => r.IsDefault);
        rules.Select(r => r.Key).Should().BeEquivalentTo(
            "BASE_COST", "MARGIN_MULTIPLIER", "DEFAULT_DEPTH_CM", "MATERIAL_default");
    }

    [Fact]
    public async Task InitializeAsync_WhenSeedRulesExistWithoutIsDefault_BackfillsThem()
    {
        SetupSkipAdminAndRoles();
        var (initializer, context) = CreateInitializerWithContext();
        context.QuotationRules.AddRange(
            new global::Domain.ProductAggregate.QuotationRule { Key = "BASE_COST", Value = 5000m },
            new global::Domain.ProductAggregate.QuotationRule { Key = "MARGIN_MULTIPLIER", Value = 1.6m },
            new global::Domain.ProductAggregate.QuotationRule { Key = "USER_RULE", Value = 10m });
        await context.SaveChangesAsync();

        await initializer.InitializeAsync();

        var rules = await context.QuotationRules.ToListAsync();
        rules.Should().HaveCount(3);
        rules.Single(r => r.Key == "BASE_COST").IsDefault.Should().BeTrue();
        rules.Single(r => r.Key == "MARGIN_MULTIPLIER").IsDefault.Should().BeTrue();
        rules.Single(r => r.Key == "USER_RULE").IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_WhenSeedRulesAlreadyDefault_NoChanges()
    {
        SetupSkipAdminAndRoles();
        var (initializer, context) = CreateInitializerWithContext();
        var existing = new global::Domain.ProductAggregate.QuotationRule { Key = "BASE_COST", Value = 5000m };
        existing.MarkAsDefault();
        context.QuotationRules.Add(existing);
        await context.SaveChangesAsync();

        await initializer.InitializeAsync();

        var rules = await context.QuotationRules.ToListAsync();
        rules.Should().HaveCount(1);
        rules[0].IsDefault.Should().BeTrue();
    }
}
