using GameTrack.Api.Identity;
using GameTrack.Api.Middleware;
using GameTrack.Domain.Models;
using GameTrack.Domain.Services;
using GameTrack.Infrastructure;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Локальные секреты и UserSecrets подключаются вне репозитория.
// Строка подключения к PostgreSQL берётся отсюда (ConnectionStrings:GameTrack).
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
        Description = "REST API библиотеки игр: DDD, PostgreSQL (SQL + EF Core), асинхронность и пагинация."
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

// Infrastructure: NpgsqlDataSource + DbContext + репозитории (SQL и EF Core).
// Domain про EF и Npgsql не знает — зависимость внедряется здесь.
builder.Services.AddPersistence(builder.Configuration);

// Текущий пользователь: Guid из X-User-Id -> логин аккаунта (middleware).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextProvider, HttpCurrentUser>();

// Domain-сервисы.
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<StatisticsService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

app.UseExceptionHandling();
app.UseRequestLogging();
app.UseCors("Frontend");
app.UseCurrentUser();
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
