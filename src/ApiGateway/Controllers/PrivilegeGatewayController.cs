using Microsoft.AspNetCore.Mvc;

namespace ApiGatewayService;

[ApiController]
[Route("api/v1/privilege")]
[Produces("application/json")]
public class PrivilegeGatewayController : ControllerBase
{
    private readonly BonusService _bonus;

    public PrivilegeGatewayController(BonusService bonus)
    {
        _bonus = bonus;
    }

    [HttpGet]
    public async Task<IActionResult> GetPrivilege(
        [FromHeader(Name = "X-User-Name")] string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { message = "User header missing" });

        try
        {
            var privilege = await _bonus.GetPrivilegeCriticalAsync(username);

            if (privilege == null)
                return StatusCode(503, new { message = "Bonus Service unavailable" });

            return Ok(privilege);
        }
        catch
        {
            return StatusCode(503, new { message = "Bonus Service unavailable" });
        }
    }
}
