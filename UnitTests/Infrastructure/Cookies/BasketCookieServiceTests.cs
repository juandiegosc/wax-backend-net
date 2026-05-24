using Infrastructure.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace UnitTests.Infrastructure.Cookies;

public class BasketCookieServiceTests
{
    private static IWebHostEnvironment CreateDevEnvironment()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        return env.Object;
    }

    private static (IHttpContextAccessor accessor, HttpContext httpContext) CreateHttpContextAccessor()
    {
        var httpContext = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(httpContext);
        return (accessor.Object, httpContext);
    }

    [Fact]
    public void GetBasketId_WhenCookieExists_ReturnsCookieValue()
    {
        var (accessor, httpContext) = CreateHttpContextAccessor();
        httpContext.Request.Headers.Cookie = "basketId=basket-123";
        var service = new BasketCookieService(accessor, CreateDevEnvironment());

        var result = service.GetBasketId();

        result.Should().Be("basket-123");
    }

    [Fact]
    public void GetBasketId_WhenCookieDoesNotExist_ReturnsNull()
    {
        var (accessor, _) = CreateHttpContextAccessor();
        var service = new BasketCookieService(accessor, CreateDevEnvironment());

        var result = service.GetBasketId();

        result.Should().BeNull();
    }

    [Fact]
    public void GetBasketId_WhenHttpContextIsNull_ReturnsNull()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var service = new BasketCookieService(accessor.Object, CreateDevEnvironment());

        var result = service.GetBasketId();

        result.Should().BeNull();
    }

    [Fact]
    public void SetBasketId_AppendsCookieToResponse()
    {
        var (accessor, httpContext) = CreateHttpContextAccessor();
        var service = new BasketCookieService(accessor, CreateDevEnvironment());
        var basketId = "new-basket-456";

        service.SetBasketId(basketId);

        var setCookieHeader = httpContext.Response.Headers.SetCookie.ToString();
        setCookieHeader.Should().Contain("basketId=new-basket-456");
    }

    [Fact]
    public void SetBasketId_SetsHttpOnlyCookie()
    {
        var (accessor, httpContext) = CreateHttpContextAccessor();
        var service = new BasketCookieService(accessor, CreateDevEnvironment());

        service.SetBasketId("test-basket");

        var setCookieHeader = httpContext.Response.Headers.SetCookie.ToString();
        setCookieHeader.Should().Contain("httponly");
    }

    [Fact]
    public void SetBasketId_WhenHttpContextIsNull_DoesNotThrow()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var service = new BasketCookieService(accessor.Object, CreateDevEnvironment());

        var act = () => service.SetBasketId("test");

        act.Should().NotThrow();
    }
}
