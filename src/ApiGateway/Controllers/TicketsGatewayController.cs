

using Microsoft.AspNetCore.Mvc;

namespace ApiGatewayService;

[ApiController]
[Route("api/v1/tickets")]
[Produces("application/json")]
public class TicketsGatewayController : ControllerBase
{
    private readonly TicketsService _service;    
    private readonly BonusService _bService;
    private readonly FlightService _fService;


    public TicketsGatewayController(TicketsService service, BonusService bService, FlightService fService)
    {
        _service = service;
        _bService = bService;
        _fService = fService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TicketResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAll(
        [FromHeader(Name = "X-User-Name")] string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { message = "User header missing" });
        try
        {
            var tickets = await _service.GetAllAsync(username);

            if (tickets == null)
                return StatusCode(503, new { message = "Ticket service unavailable" });

            return Ok(tickets);
        }
        catch
        {
            return StatusCode(503, new { message = "Ticket service unavailable" });
        }
    }

    [HttpGet("{ticketUid:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetByUid(
        Guid ticketUid,
        [FromHeader(Name = "X-User-Name")] string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { message = "User header missing" });

        try
        {
            var ticket = await _service.GetByUidAsync(ticketUid, username);

            if (ticket == null)
                return NotFound(new { message = $"Ticket {ticketUid} not found" });

            return Ok(ticket);
        }
        catch
        {
            return StatusCode(503, new { message = "Ticket service unavailable" });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(TicketPurchaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Purchase(
        [FromHeader(Name = "X-User-Name")] string? username,
        [FromBody] TicketPurchaseRequest dto)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new ValidationErrorResponse(
                "User header missing",
                new() { { "X-User-Name", "required" } }
            ));

        if (dto == null || string.IsNullOrWhiteSpace(dto.FlightNumber) || dto.Price <= 0)
            return BadRequest(new ValidationErrorResponse(
                "Invalid request",
                new()
                {
                    { "flightNumber", "required" },
                    { "price", "must be > 0" }
                }
            ));

        var result = await _service.PurchaseAsync(username, dto);

        if (result == null)
            return StatusCode(503, new ErrorResponse("Bonus Service unavailable"));

        return Ok(result);
    }

    [HttpDelete("{ticketUid:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Cancel(
        Guid ticketUid,
        [FromHeader(Name = "X-User-Name")] string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new ErrorResponse("User header missing"));

        var success = await _service.CancelAsync(ticketUid, username);
        if (!success)
            return StatusCode(503, new ErrorResponse("Ticket service unavailable"));

        return NoContent();
    }

}
