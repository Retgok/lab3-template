using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace TicketService;


[ApiController]
[Route("api/v1/tickets")]
[Produces("application/json")]
public class TicketsController : ControllerBase
{
    private readonly ITicketRepo _repo;

    public TicketsController(ITicketRepo repo)
    {
        _repo = repo;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TicketResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromHeader(Name = "X-User-Name")] string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return MissingUserHeader();

        var list = await _repo.GetAllByUserAsync(username);
        return Ok(list.Select(t => new TicketResponse(t)));
    }

    [HttpGet("{ticketUid:guid}")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByUid(
        Guid ticketUid,
        [FromHeader(Name = "X-User-Name")] string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return MissingUserHeader();

        var ticket = await _repo.GetByUidAsync(ticketUid, username);
        if (ticket == null)
            return NotFound(new ErrorResponse($"Ticket {ticketUid} not found"));

        return Ok(new TicketResponse(ticket));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TicketPurchaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Purchase(
        [FromHeader(Name = "X-User-Name")] string? username,
        [FromBody] TicketPurchaseRequest dto)
    {
        if (string.IsNullOrWhiteSpace(username))
            return MissingUserHeader();

        if (!ModelState.IsValid)
            return ValidationProblem();

        var ticket = new Ticket
        {
            TicketUid = Guid.NewGuid(),
            Username = username,
            FlightNumber = dto.FlightNumber,
            Price = dto.Price,
            Status = "PAID"
        };

        await _repo.AddAsync(ticket);

        return Ok(new TicketPurchaseResponse(ticket, paidByMoney: dto.Price, paidByBonuses: 0));
    }

    [HttpDelete("{ticketUid:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(
        Guid ticketUid,
        [FromHeader(Name = "X-User-Name")] string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return MissingUserHeader();

        var ticket = await _repo.GetByUidAsync(ticketUid, username);
        if (ticket == null)
            return NotFound(new ErrorResponse($"Ticket {ticketUid} not found"));

        ticket.Status = "CANCELED";
        await _repo.UpdateAsync(ticket);

        return NoContent();
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
