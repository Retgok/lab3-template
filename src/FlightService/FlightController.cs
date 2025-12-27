using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FlightService;

[ApiController]
[Route("api/v1/flights")]
[Produces("application/json")]
public class FlightsController : ControllerBase
{
    private readonly IFlightRepo _repo;
    public FlightsController(IFlightRepo repo) { _repo = repo; }

    [HttpGet]
    [ProducesResponseType(typeof(List<FlightResponse>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 100)
    {
        var list = await _repo.GetAllAsync(page, size);
        return Ok(list.Select(f => new FlightResponse(f)));
    }

    [HttpGet("{flightNumber}")]
    [ProducesResponseType(typeof(FlightResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByFlightNumber(
        [FromRoute] string flightNumber,
        [FromHeader(Name = "X-User-Name")] string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return MissingUserHeader();

        var flight = await _repo.GetByFlightNumberAsync(flightNumber);
        if (flight == null)
            return NotFound(new ErrorResponse($"Flight {flightNumber} not found"));

        return Ok(new FlightResponse(flight));
    }

    private BadRequestObjectResult MissingUserHeader()
    {
        return BadRequest(new ValidationErrorResponse(
            "User header missing",
            new Dictionary<string, string> { { "X-User-Name", "required" } }));
    }

    public override ActionResult ValidationProblem()
    {
        var errors = ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => string.Join(";", e.Value!.Errors.Select(er => er.ErrorMessage))
            );

        return BadRequest(new ValidationErrorResponse("Invalid data", errors));
    }
}

[ApiController]
[Route("manage")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok("OK");
    }
}