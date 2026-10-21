using GameTrack.Api.Identity;
using GameTrack.Api.Middleware;
using GameTrack.Domain.Models;
using GameTrack.Domain.Repositories;
using GameTrack.Domain.Services;
using GameTrack.Infrastructure.Persistence;
using GameTrack.Infrastructure.Security;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Локальные секреты и UserSecrets подключаются вне репозитория.
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GameTrack API",
        Version = "v1",
        Description = "REST API библиотеки игр: DDD, асинхронные операции, пагинация и валидация."
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, "GameTrack.Api.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var pagination = builder.Configuration.GetSection("Pagination").Get<PaginationOptions>()
                 ?? new PaginationOptions();
pagination.EnsureValid();
builder.Services.AddSingleton(pagination);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

// Infrastructure: единые in-memory репозитории на всё время жизни приложения.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddSingleton(new InMemoryUserGameRepositoryProvider(SeedData.ProfileId));
builder.Services.AddScoped<IGameRepository>(serviceProvider =>
{
    var currentUser = serviceProvider.GetRequiredService<ICurrentUser>();
    var provider = serviceProvider.GetRequiredService<InMemoryUserGameRepositoryProvider>();
    return provider.GetRepository(currentUser.UserId);
});
builder.Services.AddSingleton<IUserProfileRepository, InMemoryUserProfileRepository>();

var demoEmail = builder.Configuration["DemoAuth:Email"];
var demoPassword = builder.Configuration["DemoAuth:Password"];
IAccountRepository accounts = !string.IsNullOrWhiteSpace(demoEmail) && !string.IsNullOrWhiteSpace(demoPassword)
    ? new InMemoryAccountRepository(
        demoEmail,
        demoPassword,
        id: SeedData.ProfileId)
    : new InMemoryAccountRepository();
builder.Services.AddSingleton(accounts);

// Domain-сервисы.
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<StatisticsService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

app.UseExceptionHandling();
app.UseRequestLogging();
app.UseCors("Frontend");
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "GameTrack API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "GameTrack API";
});

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

/// <summary>Точка входа, доступная для интеграционных тестов.</summary>
public partial class Program
{
}
