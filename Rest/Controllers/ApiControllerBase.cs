using Microsoft.AspNetCore.Mvc;
using Rest.Common;

namespace Rest.Controllers;

/// <summary>
/// Base controller with common functionality for API controllers.
/// </summary>
[ApiController]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Creates a ProblemDetails response from an Error.
    /// </summary>
    protected static ProblemDetails ToProblemDetails(Error error) =>
        new()
        {
            Title = error.Code,
            Detail = error.Description,
            Type = error.Code
        };

    /// <summary>
    /// Creates a BadRequest response with ProblemDetails.
    /// </summary>
    protected ObjectResult BadRequestProblem(Error error) =>
        BadRequest(ToProblemDetails(error));

    /// <summary>
    /// Creates an Unauthorized response with ProblemDetails.
    /// </summary>
    protected ObjectResult UnauthorizedProblem(Error error) =>
        Unauthorized(ToProblemDetails(error));

    /// <summary>
    /// Creates a NotFound response with ProblemDetails.
    /// </summary>
    protected ObjectResult NotFoundProblem(Error error) =>
        NotFound(ToProblemDetails(error));
}
