using AssetPulse.Api.Data;
using AssetPulse.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
builder.Services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
{
    var problemDetails = new ValidationProblemDetails(context.ModelState)
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "One or more validation errors occurred.",
        Instance = context.HttpContext.Request.Path
    };
    problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    return new ObjectResult(problemDetails) { StatusCode = StatusCodes.Status400BadRequest };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<AssetPulseDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("AssetPulse")
        ?? "Server=127.0.0.1,1433;Database=AssetPulse;User Id=sa;TrustServerCertificate=True"));
builder.Services.AddScoped<DevelopmentDataSeeder>();
builder.Services.AddScoped<IAssetReadService, AssetReadService>();
builder.Services.AddScoped<IAssetWriteService, AssetWriteService>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (exception is not null)
    {
        app.Logger.LogError(exception, "Unhandled exception for {Path}", context.Request.Path);
    }

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await Results.Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "An unexpected error occurred.",
        detail: "The server could not complete the request.",
        instance: context.Request.Path,
        extensions: new Dictionary<string, object?>
        {
            ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
        }).ExecuteAsync(context);
}));

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/api/health");
app.MapControllers();

if (args.Contains("--seed-development-data", StringComparer.OrdinalIgnoreCase))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Development seed data can only be applied in the Development environment.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AssetPulseDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();

    await dbContext.Database.MigrateAsync();
    await seeder.SeedAsync();
    return;
}

app.Run();

public partial class Program
{
}
