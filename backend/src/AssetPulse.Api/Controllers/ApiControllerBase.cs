using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AssetPulse.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    protected static bool IsDefined<TEnum>(TEnum? value)
        where TEnum : struct, Enum => value is null || Enum.IsDefined(value.Value);

    protected ObjectResult NotFoundProblem(string detail) => ProblemResult(new ProblemDetails
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Resource not found",
        Detail = detail,
        Instance = HttpContext.Request.Path
    });

    protected ObjectResult InvalidFilterProblem(string field, string message) =>
        ValidationProblemResult(new Dictionary<string, string[]> { [field] = [message] });

    protected ObjectResult ValidationProblemResult(Dictionary<string, string[]> errors) => ProblemResult(
        new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Detail = "One or more query parameters are invalid.",
            Instance = HttpContext.Request.Path
        });

    private ObjectResult ProblemResult(ProblemDetails problemDetails)
    {
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return new ObjectResult(problemDetails) { StatusCode = problemDetails.Status };
    }
}
