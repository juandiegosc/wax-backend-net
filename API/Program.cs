using API.Middleware;
using API.SignalR;
using Application.Basket.Commands;
using Application.Basket.Interfaces;
using Application.Core.Validations;
using Application.Interfaces;
using Application.Interfaces.Publish;
using Application.Interfaces.Repositories.ReadRepositories;
using Application.Interfaces.Repositories.WriteRepositories;
using Application.Interfaces.Services;
using Application.Product.Commands.Delete;
using Application.Reports.Queries;
using Application.Reports.Validators;
using Domain.Entities;
using Domain.Enumerators;
using FluentValidation;
using Infrastructure.Billing;
using Infrastructure.Cookies;
using Infrastructure.Email;
using Infrastructure.Email.EmailTemplates;
using Infrastructure.Email.Services;
using Infrastructure.Images;
using Infrastructure.Messaging;
using Infrastructure.Payments;
using Infrastructure.Quotation;
using Infrastructure.Repositories.ReadRepositories;
using Infrastructure.Repositories.WriteRepositories;
using Infrastructure.Security;
using MassTransit;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence;
using Resend;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddDbContext<WriteDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("WriteConnection"));
});

builder.Services.AddDbContext<ReadDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("ReadConnection"));
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("Pagination", "NextCursor")
              .WithOrigins(allowedOrigins);
    });
});
builder.Services.AddSignalR();

builder.Services.AddMediatR(x =>
{
    x.RegisterServicesFromAssemblyContaining<AddItemCommandHandler>();
    x.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddMassTransit(configuration =>
{
    configuration.AddConsumers(typeof(Infrastructure.AssemblyMarker).Assembly);
    configuration.AddEntityFrameworkOutbox<WriteDbContext>(options =>
    {
        options.UsePostgres();
        options.UseBusOutbox();
    });
    configuration.UsingRabbitMq((context, config) =>
    {
        config.Host(builder.Configuration["RabbitMQ:Host"], host =>
        {
            host.Username(builder.Configuration["RabbitMQ:Username"]!);
            host.Password(builder.Configuration["RabbitMQ:Password"]!);
        });
        
        config.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
        config.ConfigureEndpoints(context);
    });
});

builder.Services.AddOptions();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<ResendClientOptions>(o =>
    o.ApiToken = builder.Configuration["EmailSettings:ApiToken"]!);

builder.Services.AddSingleton<IOptionsSnapshot<ResendClientOptions>,
    StaticOptionsSnapshot<ResendClientOptions>>();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddTransient<IEmailService, ResendEmailService>();
builder.Services.AddTransient<IEmailSender<User>, IdentityEmailSender>();

builder.Services.Configure<FacturaPlanSettings>(
    builder.Configuration.GetSection("FacturaPlan"));

builder.Services.AddHttpClient<IBillingFacade, FacturaPlanBillingFacade>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<FacturaPlanSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
});

builder.Services.AddSingleton<FacturaPlanRequestMapper>();

builder.Services.AddScoped<IUserAccessor, UserAccessor>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IBasketProvider, BasketCookieService>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IBasketRepository, BasketRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IQuotationRuleRepository, QuotationRuleRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IProductReadRepository, ProductReadRepository>();
builder.Services.AddScoped<ICustomProductRepository, CustomProductRepository>();
builder.Services.AddScoped<ICustomProductReadRepository, CustomProductReadRepository>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IQuotationRulesCache, QuotationRulesCache>();
builder.Services.AddScoped<IDimensionParser, DimensionParser>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<IOrderReadRepository, OrderReadRepository>();
builder.Services.AddScoped<ISupportTicketReadRepository, SupportTicketReadRepository>();

builder.Services.AddScoped<IEventPublisher, EventPublisher>();

builder.Services.AddScoped<IProductDeletionStrategy, CatalogProductDeletionStrategy>();
builder.Services.AddScoped<IProductDeletionStrategy, CustomProductDeletionStrategy>();

// Validators del modulo de reporteria (registro explicito — ADR-5).
// ValidationBehavior recibe IValidator<T>? como opcional; sin registro explicito la validacion no se ejecuta.
builder.Services.AddScoped<IValidator<GetOrdersByDateReportQuery>, GetOrdersByDateReportValidator>();
builder.Services.AddScoped<IValidator<GetTopBuyersReportQuery>, GetTopBuyersReportValidator>();
builder.Services.AddScoped<IValidator<GetProductsStockReportQuery>, GetProductsStockReportValidator>();
builder.Services.AddScoped<IValidator<GetSupportByDateReportQuery>, GetSupportByDateReportValidator>();

// Validators del modulo de administracion de reglas de cotizacion.
builder.Services.AddScoped<IValidator<Application.Quotation.Commands.CreateQuotationRuleCommand>, Application.Quotation.Validators.CreateQuotationRuleCommandValidator>();
builder.Services.AddScoped<IValidator<Application.Quotation.Commands.UpdateQuotationRuleCommand>, Application.Quotation.Validators.UpdateQuotationRuleCommandValidator>();
builder.Services.AddScoped<IValidator<Application.Quotation.Commands.DeleteQuotationRuleCommand>, Application.Quotation.Validators.DeleteQuotationRuleCommandValidator>();

builder.Services.AddIdentityApiEndpoints<User>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<WriteDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".AspNetCore.Identity.Application";

    if (builder.Environment.IsProduction())
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.Domain = ".waxweb.shop";
    }
    else
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    }
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RegisterOrAdmin", policy =>
        policy.RequireRole(Roles.Admin, Roles.Registered));
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapGroup("api").MapIdentityApi<User>();
app.MapHub<SupportHub>("/comments");

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var writeContext = services.GetRequiredService<WriteDbContext>();
    var readContext = services.GetRequiredService<ReadDbContext>();

    const int maxAttempts = 10;
    var attempt = 1;
    while (attempt <= maxAttempts)
    {
        try
        {
            await writeContext.Database.MigrateAsync();
            await readContext.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts && IsTransientConnectionError(ex))
        {
            services.GetRequiredService<ILogger<Program>>()
                .LogWarning(ex, "Postgres not ready on attempt {Attempt}, retrying", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
            attempt++;
        }
    }
    
    var initializer = new DbInitializer(
        services.GetRequiredService<UserManager<User>>(),
        services.GetRequiredService<RoleManager<IdentityRole>>(),
        services.GetRequiredService<IConfiguration>(),
        services.GetRequiredService<WriteDbContext>(),
        services.GetRequiredService<ILogger<DbInitializer>>());
    await initializer.InitializeAsync();
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred during migration and seeding data");
}

await app.RunAsync();

static bool IsTransientConnectionError(Exception ex)
{
    for (var current = ex; current is not null; current = current.InnerException)
    {
        if (current is Npgsql.NpgsqlException or System.Net.Sockets.SocketException
            or TimeoutException)
        {
            return true;
        }
    }
    return false;
}
