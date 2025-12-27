using Microsoft.AspNetCore.Mvc;

namespace ApiGatewayService;

[ApiController]
[Route("api/v1/flights")]
[Produces("application/json")]
public class FlightsGatewayController : ControllerBase
{
    private readonly FlightService _service;

    public FlightsGatewayController(FlightService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFlights(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        if (size < 1 || size > 100)
            return BadRequest(new { message = "Invalid paging parameters" });

        try
        {
            var result = await _service.GetFlightsAsync(page, size);

            if (result == null)
                return StatusCode(500, new { message = "Flight service unavailable" });

            return Ok(result);
        }
        catch
        {
            return StatusCode(500, new { message = "Flight service unavailable" });
        }
    }
}
