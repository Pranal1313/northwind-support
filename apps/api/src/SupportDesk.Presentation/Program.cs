using System.Text.Json.Serialization;
using SupportDesk.Application;
using SupportDesk.Infrastructure;
using SupportDesk.Presentation.Extensions;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Presentation.Filters;
using SupportDesk.Presentation.Middleware;
using SupportDesk.Application.Configuration;
using SupportDesk.Application.Services;


var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "frontend";

builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options =>
    {
        // Enums travel as their names, both ways, so the API reads well from the browser.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCors(options => options.AddPolicy(
    FrontendCorsPolicy,
    policy => policy
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services
    .AddOptions<SlaOptions>()
    .Bind(builder.Configuration.GetSection(SlaOptions.SectionName))
    .Validate(
        options => options.BaseWindowHours.Count > 0
            && options.BaseWindowHours.Values.All(hours => hours > 0)
            && options.MinimumWindowHours > 0
            && options.PremiumCustomerMultiplier > 0
            && options.PremiumCustomerMultiplier <= 1
            && options.AtRiskThresholdPercent >= 0
            && options.AtRiskThresholdPercent <= 100,
        "SLA configuration contains invalid values.")
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SlaOptions>>().Value);

builder.Services.AddSingleton<SlaWindowCalculator>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Keeps a developer machine one command away from a working database.
    await app.MigrateAndSeedAsync();
}
app.MapGet("/health", async (
    SupportDbContext db,
    CancellationToken cancellationToken) =>
{
    var canConnect = await db.Database.CanConnectAsync(cancellationToken);

    return canConnect
        ? Results.Ok(new { status = "Healthy" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();
app.UseCors(FrontendCorsPolicy);

app.MapControllers();

app.Run();
