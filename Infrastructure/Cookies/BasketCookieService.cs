using Application.Basket.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Cookies;

public class BasketCookieService(IHttpContextAccessor contextAccessor, IWebHostEnvironment env) : IBasketProvider
{
    private const string BasketIdCookieName = "basketId";
    
    public string? GetBasketId()
    {
        return contextAccessor.HttpContext?.Request.Cookies[BasketIdCookieName];
    }

    public void SetBasketId(string basketId)
    {
        CookieOptions cookieOptions;

        if (env.IsProduction())
        {
            cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Domain = ".waxweb.shop",
                Expires = DateTime.UtcNow.AddDays(30),
            };
        }
        else
        {
            cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(30),
            };
        }
        
        contextAccessor.HttpContext?.Response.Cookies.Append(BasketIdCookieName, basketId, cookieOptions);
    }

    public void DeleteBasketId()
    {
        CookieOptions cookieOptions;

        if (env.IsProduction())
        {
            cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Domain = ".waxweb.shop",
            };
        }
        else
        {
            cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
            };
        }

        contextAccessor.HttpContext?.Response.Cookies.Delete(BasketIdCookieName, cookieOptions);
    }
}