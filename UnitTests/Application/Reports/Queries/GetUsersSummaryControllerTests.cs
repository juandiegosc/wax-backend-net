using API.Controllers;
using Application.Reports.DTOs;
using Domain.Enumerators;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MockQueryable;
using UnitTests.Helpers;
using DomainUser = Domain.Entities.User;

namespace UnitTests.Application.Reports.Queries;

public class GetUsersSummaryControllerTests
{
    private static Mock<UserManager<DomainUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<DomainUser>>();
        return new Mock<UserManager<DomainUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    // RFC-16-A: conteo total y desglose por rol correctos
    [Fact]
    public async Task GetUsersSummary_ConUsuariosEnVariosRoles_RetornaConteoCorrecto()
    {
        var admin = new DomainUser { Id = "admin", Email = "admin@test.com", EmailConfirmed = true };
        var registered = Enumerable.Range(0, 20)
            .Select(i => new DomainUser { Id = $"r{i}", EmailConfirmed = i % 2 == 0 })
            .ToList();
        var enrolled = Enumerable.Range(0, 5)
            .Select(i => new DomainUser { Id = $"e{i}", EmailConfirmed = false })
            .ToList();

        var todos = new List<DomainUser> { admin };
        todos.AddRange(registered);
        todos.AddRange(enrolled);

        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.Users).Returns(todos.BuildMock());
        userManager.Setup(m => m.GetRolesAsync(admin)).ReturnsAsync([Roles.Admin]);
        foreach (var u in registered)
        {
            userManager.Setup(m => m.GetRolesAsync(u)).ReturnsAsync([Roles.Registered]);
        }
        foreach (var u in enrolled)
        {
            userManager.Setup(m => m.GetRolesAsync(u)).ReturnsAsync([Roles.Enrolled]);
        }

        var (_, controller) = ControllerTestFactory.Create(new ReportsController(userManager.Object));

        var result = await controller.GetUsersSummary();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<UsersSummaryReportDto>().Subject;

        dto.TotalUsers.Should().Be(26);
        dto.ByRole[Roles.Admin].Should().Be(1);
        dto.ByRole[Roles.Registered].Should().Be(20);
        dto.ByRole[Roles.Enrolled].Should().Be(5);
        // admin (1) + registered con indice par (10) = 11 emails confirmados
        dto.ConfirmedEmails.Should().Be(11);
    }

    [Fact]
    public async Task GetUsersSummary_SinUsuarios_RetornaCeros()
    {
        var userManager = CreateUserManagerMock();
        userManager.Setup(m => m.Users).Returns(new List<DomainUser>().BuildMock());

        var (_, controller) = ControllerTestFactory.Create(new ReportsController(userManager.Object));

        var result = await controller.GetUsersSummary();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<UsersSummaryReportDto>().Subject;

        dto.TotalUsers.Should().Be(0);
        dto.ConfirmedEmails.Should().Be(0);
        dto.ByRole[Roles.Admin].Should().Be(0);
        dto.ByRole[Roles.Registered].Should().Be(0);
        dto.ByRole[Roles.Enrolled].Should().Be(0);
    }
}
