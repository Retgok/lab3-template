using Microsoft.AspNetCore.Mvc;

namespace BonusService;

[ApiController]
[Route("api/v1/privilege")]
[Produces("application/json")]
public class BonusController : ControllerBase
{
    private readonly IBonusRepo _repo;

    public BonusController(IBonusRepo repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromHeader(Name = "X-User-Name")] string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { error = "User header missing" });

        var privilege = await _repo.GetByUsernameAsync(username);

        if (privilege == null)
        {
            privilege = new Privilege { Username = username, Balance = 0, Status = "BRONZE" };
            await _repo.AddPrivilegeAsync(privilege);
        }

        return Ok(new PrivilegeResponse(privilege));
    }

    [HttpPost("apply")]
    public async Task<IActionResult> ApplyBonus(
        [FromHeader(Name = "X-User-Name")] string? username,
        [FromBody] ApplyBonusRequest req)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { error = "User header missing" });

        var privilege = await _repo.GetByUsernameAsync(username);
        if (privilege == null)
        {
            privilege = new Privilege { Username = username, Balance = 0, Status = "BRONZE" };
            await _repo.AddPrivilegeAsync(privilege);
        }

        int usedBonus = 0;
        int paidByMoney = req.Price;

        if (req.PaidFromBalance)
        {
            usedBonus = Math.Min(privilege.Balance, req.Price);
            paidByMoney -= usedBonus;
            privilege.Balance -= usedBonus;

            await _repo.AddHistoryAsync(new PrivilegeHistory
            {
                PrivilegeId = privilege.Id,
                TicketUid = req.TicketUid,
                DateTime = DateTime.UtcNow,
                BalanceDiff = -usedBonus,
                OperationType = "DEBIT_THE_ACCOUNT"
            });
        }
        else
        {
            int bonus = (int)(req.Price * 0.1);
            privilege.Balance += bonus;

            await _repo.AddHistoryAsync(new PrivilegeHistory
            {
                PrivilegeId = privilege.Id,
                TicketUid = req.TicketUid,
                DateTime = DateTime.UtcNow,
                BalanceDiff = bonus,
                OperationType = "FILL_IN_BALANCE"
            });
        }

        privilege.Status = privilege.Balance switch
        {
            >= 10000 => "GOLD",
            >= 5000 => "SILVER",
            _ => "BRONZE"
        };

        await _repo.UpdatePrivilegeAsync(privilege);

        return Ok(new { paidByMoney, paidByBonuses = usedBonus });
    }

    [HttpPost("refund/{ticketUid:guid}")]
    public async Task<IActionResult> Refund(
        [FromHeader(Name = "X-User-Name")] string? username,
        Guid ticketUid)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { error = "User header missing" });

        var lastHistory = await _repo.GetLastHistoryByTicketAsync(ticketUid);
        if (lastHistory == null)
            return NotFound(new { error = "No history found for ticket" });

        var privilege = lastHistory.Privilege;

        int delta = lastHistory.OperationType switch
        {
            "DEBIT_THE_ACCOUNT" => lastHistory.BalanceDiff,
            "FILL_IN_BALANCE" => -lastHistory.BalanceDiff,
            "FILLED_BY_MONEY" => 0,
            _ => 0
        };

        if (delta == 0)
            return NoContent();

        privilege.Balance += delta;
        if (privilege.Balance < 0) privilege.Balance = 0;

        await _repo.AddHistoryAsync(new PrivilegeHistory
        {
            PrivilegeId = privilege.Id,
            TicketUid = ticketUid,
            DateTime = DateTime.UtcNow,
            BalanceDiff = Math.Abs(delta),
            OperationType = delta > 0 ? "FILL_IN_BALANCE" : "DEBIT_THE_ACCOUNT"
        });

        privilege.Status = privilege.Balance switch
        {
            >= 10000 => "GOLD",
            >= 5000 => "SILVER",
            _ => "BRONZE"
        };

        await _repo.UpdatePrivilegeAsync(privilege);

        return NoContent();
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


public class ApplyBonusRequest
{
    public Guid TicketUid { get; set; }
    public int Price { get; set; }
    public bool PaidFromBalance { get; set; }
}
