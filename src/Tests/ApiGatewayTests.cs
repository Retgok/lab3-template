using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System;
using ApiGatewayService;

namespace ApiGatewayService
{
    public class FlightItemStub
    {
        public int Id { get; set; }
        public string FlightNumber { get; set; } = default!;
    }
    
    public class PaginationResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalElements { get; set; }
        public List<FlightItemStub> Items { get; set; } = new List<FlightItemStub>();
    }
    
    public class FlightsClient
    {
        public virtual Task<PaginationResponse?> GetAllAsync(int page, int size) => throw new NotImplementedException();
        public virtual Task<FlightClientModel?> GetByFlightNumberAsync(string flightNumber, string username) => throw new NotImplementedException();
    }
    
    [ApiController]
    [Route("api/v1/flights")]
    public class FlightsGatewayController : ControllerBase
    {
        private readonly FlightsClient _flights;

        public FlightsGatewayController(FlightsClient flights)
        {
            _flights = flights;
        }

        [HttpGet]
        public async Task<IActionResult> GetFlights([FromQuery] int page = 0, [FromQuery] int size = 10)
        {
            if (page < 0 || size < 1 || size > 100)
                return BadRequest(new { message = "Invalid paging parameters" });

            var result = await _flights.GetAllAsync(page, size);

            if (result == null)
                return BadRequest(new { message = "Unable to fetch flights" });

            return Ok(result);
        }
    }
}

public class FlightsGatewayControllerTests
{
    private readonly Mock<FlightsClient> _flightsClientMock;
    private readonly FlightsGatewayController _controller;

    public FlightsGatewayControllerTests()
    {
        _flightsClientMock = new Mock<FlightsClient>();
        _controller = new FlightsGatewayController(_flightsClientMock.Object);
    }

    [Fact]
    public async Task GetFlights_ReturnsOk_WithValidPagination()
    {
        var mockResponse = new PaginationResponse
        {
            Page = 0,
            PageSize = 10,
            TotalElements = 50,
            Items = new List<FlightItemStub> 
            { 
                new FlightItemStub { Id = 1, FlightNumber = "SU100" }, 
                new FlightItemStub { Id = 2, FlightNumber = "SU101" } 
            }
        };

        _flightsClientMock.Setup(c => c.GetAllAsync(0, 10)).ReturnsAsync(mockResponse);

        var result = await _controller.GetFlights(0, 10);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PaginationResponse>(okResult.Value);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(50, response.TotalElements);
        Assert.Equal(2, response.Items.Count);

        _flightsClientMock.Verify(c => c.GetAllAsync(0, 10), Times.Once);
    }

    [Fact]
    public async Task GetFlights_UsesDefaultParameters_WhenNoneProvided()
    {
        var mockResponse = new PaginationResponse { Page = 0, PageSize = 10, TotalElements = 100 };
        _flightsClientMock.Setup(c => c.GetAllAsync(0, 10)).ReturnsAsync(mockResponse);

        var result = await _controller.GetFlights();

        Assert.IsType<OkObjectResult>(result);
        _flightsClientMock.Verify(c => c.GetAllAsync(0, 10), Times.Once);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 0)]
    [InlineData(0, 101)]
    public async Task GetFlights_ReturnsBadRequest_ForInvalidParameters(int page, int size)
    {
        var result = await _controller.GetFlights(page, size);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        _flightsClientMock.Verify(c => c.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetFlights_ReturnsBadRequest_WhenClientReturnsNull()
    {
        _flightsClientMock.Setup(c => c.GetAllAsync(0, 10)).ReturnsAsync((PaginationResponse?)null);

        var result = await _controller.GetFlights(0, 10);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        
        var value = (dynamic)badRequestResult.Value!;
        Assert.Contains("Unable to fetch flights", (string)value.message);
        
        _flightsClientMock.Verify(c => c.GetAllAsync(0, 10), Times.Once);
    }
}

namespace ApiGatewayService
{
    public class ErrorResponse { public string Message { get; set; } public ErrorResponse(string msg) => Message = msg; }
    public class ValidationErrorResponse { public string Message { get; set; } public Dictionary<string, string> Errors { get; set; } public ValidationErrorResponse(string msg, Dictionary<string, string> errors) { Message = msg; Errors = errors; } }
    public class PrivilegeShortInfo { public int Balance { get; set; } public string Status { get; set; } = default!; }
    public class ApplyBonusRequest { public Guid TicketUid { get; set; } public int Price { get; set; } public bool PaidFromBalance { get; set; } }
    public class BonusApplyResponse { public int PaidByMoney { get; set; } public int PaidByBonuses { get; set; } }

    public class TicketPurchaseRequest { public string FlightNumber { get; set; } = default!; public int Price { get; set; } public bool PaidFromBalance { get; set; } }
    
    public class TicketResponse
    {
        public Guid TicketUid { get; set; }
        public string FlightNumber { get; set; } = default!;
        public string FromAirport { get; set; } = default!;
        public string ToAirport { get; set; } = default!;
        public string Date { get; set; } = default!;
        public int Price { get; set; }
        public string Status { get; set; } = default!;
    }

    public class TicketPurchaseResponse : TicketResponse
    {
        public int PaidByMoney { get; set; }
        public int PaidByBonuses { get; set; }
        public PrivilegeShortInfo? Privilege { get; set; }
    }

    public class TicketClientModel { public Guid TicketUid { get; set; } public string FlightNumber { get; set; } = default!; public int Price { get; set; } public string Status { get; set; } = default!; }
    public class FlightClientModel { public string FlightNumber { get; set; } = default!; public string FromAirport { get; set; } = default!; public string ToAirport { get; set; } = default!; public DateTime Date { get; set; } }
    public class PrivilegeClientModel { public int Balance { get; set; } public string Status { get; set; } = default!; }

    public class TicketsClient
    {
        public virtual Task<List<TicketClientModel>?> GetAllByUserAsync(string username) => throw new NotImplementedException();
        public virtual Task<TicketClientModel?> GetByUidAsync(Guid ticketUid, string username) => throw new NotImplementedException();
        public virtual Task<TicketClientModel?> PurchaseAsync(string username, TicketPurchaseRequest request) => throw new NotImplementedException();
        public virtual Task<bool> CancelAsync(Guid ticketUid, string username) => throw new NotImplementedException();
    }

    public class BonusClient
    {
        public virtual Task<PrivilegeClientModel?> GetPrivilegeAsync(string username) => throw new NotImplementedException();
        public virtual Task<BonusApplyResponse?> ApplyAsync(string username, ApplyBonusRequest request) => throw new NotImplementedException();
        public virtual Task RefundAsync(string username, Guid ticketUid) => throw new NotImplementedException();
    }

    [ApiController]
    [Route("api/v1/tickets")]
    [Produces("application/json")]
    public class TicketsGatewayController : ControllerBase
    {
        private readonly TicketsClient _tickets;
        private readonly BonusClient _bonus;
        private readonly FlightsClient _flights;

        public TicketsGatewayController(TicketsClient tickets, BonusClient bonus, FlightsClient flights)
        {
            _tickets = tickets;
            _bonus = bonus;
            _flights = flights;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromHeader(Name = "X-User-Name")] string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { message = "User header missing", errors = new { XUserName = "required" } });

            var tickets = await _tickets.GetAllByUserAsync(username);
            if (tickets == null) 
                return BadRequest(new { message = "Unable to fetch tickets" });

            var fullResponses = new List<TicketResponse>();

            foreach (var t in tickets)
            {
                var flightInfo = await _flights.GetByFlightNumberAsync(t.FlightNumber, username);
                if (flightInfo == null)
                {
                    continue;
                }

                fullResponses.Add(new TicketResponse
                {
                    TicketUid = t.TicketUid,
                    FlightNumber = t.FlightNumber,
                    FromAirport = flightInfo.FromAirport,
                    ToAirport = flightInfo.ToAirport,
                    Date = flightInfo.Date.ToString("yyyy-MM-dd HH:mm"),
                    Price = t.Price,
                    Status = t.Status
                });
            }

            return Ok(fullResponses);
        }

        [HttpGet("{ticketUid:guid}")]
        public async Task<IActionResult> GetByUid(
            Guid ticketUid,
            [FromHeader(Name = "X-User-Name")] string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { message = "User header missing" });

            var ticket = await _tickets.GetByUidAsync(ticketUid, username);
            if (ticket == null)
                return NotFound(new { message = $"Ticket {ticketUid} not found" });

            var flightInfo = await _flights.GetByFlightNumberAsync(ticket.FlightNumber, username);
            if (flightInfo == null)
                return NotFound(new { message = $"Flight {ticket.FlightNumber} not found" });

            var privilege = await _bonus.GetPrivilegeAsync(username);

            var response = new
            {
                ticketUid = ticket.TicketUid,
                flightNumber = ticket.FlightNumber,
                fromAirport = flightInfo.FromAirport,
                toAirport = flightInfo.ToAirport,
                date = flightInfo.Date.ToString("yyyy-MM-dd HH:mm"),
                price = ticket.Price,
                status = ticket.Status,
                privilege
            };

            return Ok(response);
        }


        [HttpPost]
        public async Task<IActionResult> Purchase(
            [FromHeader(Name = "X-User-Name")] string? username,
            [FromBody] TicketPurchaseRequest dto)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new ValidationErrorResponse(
                    "User header missing",
                    new Dictionary<string, string>
                    {
                        { "X-User-Name", "required" }
                    }
                ));
            }

            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.FlightNumber) ||
                dto.Price <= 0)
            {
                return BadRequest(new ValidationErrorResponse(
                    "Invalid request",
                    new Dictionary<string, string>
                    {
                        { "flightNumber", "required" },
                        { "price", "must be > 0" }
                    }
                ));
            }

            var flight = await _flights.GetByFlightNumberAsync(dto.FlightNumber, username);
            if (flight == null)
            {
                return NotFound(new ErrorResponse($"Flight {dto.FlightNumber} not found"));
            }

            var ticketCreated = await _tickets.PurchaseAsync(username, dto);
            if (ticketCreated == null)
            {
                return BadRequest(new ErrorResponse("Failed to create ticket"));
            }

            var ticketUid = ticketCreated.TicketUid;

            var bonusApply = await _bonus.ApplyAsync(username, new ApplyBonusRequest
            {
                TicketUid = ticketUid,
                Price = dto.Price,
                PaidFromBalance = dto.PaidFromBalance
            });

            if (bonusApply == null)
            {
                await _tickets.CancelAsync(ticketUid, username);
                return BadRequest(new ErrorResponse("Failed to apply bonuses"));
            }

            var privilege = await _bonus.GetPrivilegeAsync(username);

            var response = new TicketPurchaseResponse
            {
                TicketUid = ticketUid,
                FlightNumber = dto.FlightNumber,
                FromAirport = flight.FromAirport,
                ToAirport = flight.ToAirport,
                Date = flight.Date.ToString("yyyy-MM-dd HH:mm"),
                Price = dto.Price,
                PaidByMoney = bonusApply.PaidByMoney,
                PaidByBonuses = bonusApply.PaidByBonuses,
                Status = ticketCreated.Status ?? "PAID",
                Privilege = privilege == null ? null : new PrivilegeShortInfo
                {
                    Balance = privilege.Balance,
                    Status = privilege.Status
                }
            };

            return Ok(response);
        }


        [HttpDelete("{ticketUid:guid}")]
        public async Task<IActionResult> Cancel(
            Guid ticketUid,
            [FromHeader(Name = "X-User-Name")] string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { message = "User header missing" });

            var ticket = await _tickets.GetByUidAsync(ticketUid, username);
            if (ticket == null)
                return NotFound(new { message = $"Ticket {ticketUid} not found" });

            var canceled = await _tickets.CancelAsync(ticketUid, username);
            if (!canceled)
                return NotFound(new { message = $"Ticket {ticketUid} not found" });

            await _bonus.RefundAsync(username, ticketUid);

            return NoContent();
        }
    }
}

public class TicketsGatewayControllerTests
{
    private readonly Mock<TicketsClient> _ticketsClientMock;
    private readonly Mock<BonusClient> _bonusClientMock;
    private readonly Mock<FlightsClient> _flightsClientMock;
    private readonly TicketsGatewayController _controller;

    private const string TestUsername = "test_user";
    private readonly Guid TestTicketUid = new Guid("4b14d59a-2423-4555-8938-1a5c60959828");
    private const string TestFlightNumber = "SU201";
    private readonly DateTime TestFlightDate = new DateTime(2025, 12, 30, 10, 0, 0, DateTimeKind.Utc);

    public TicketsGatewayControllerTests()
    {
        _ticketsClientMock = new Mock<TicketsClient>();
        _bonusClientMock = new Mock<BonusClient>();
        _flightsClientMock = new Mock<FlightsClient>();
        _controller = new TicketsGatewayController(_ticketsClientMock.Object, _bonusClientMock.Object, _flightsClientMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsBadRequest_WhenUsernameIsMissing()
    {
        var result = await _controller.GetAll(null);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var value = (dynamic)badRequestResult.Value!;
        Assert.Equal("User header missing", (string)value.message);
    }

    [Fact]
    public async Task GetAll_ReturnsBadRequest_WhenTicketsClientFails()
    {
        _ticketsClientMock.Setup(c => c.GetAllByUserAsync(TestUsername)).ReturnsAsync((List<TicketClientModel>?)null);

        var result = await _controller.GetAll(TestUsername);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var value = (dynamic)badRequestResult.Value!;
        Assert.Equal("Unable to fetch tickets", (string)value.message);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithFullTicketsList()
    {
        var tickets = new List<TicketClientModel>
        {
            new TicketClientModel { TicketUid = TestTicketUid, FlightNumber = TestFlightNumber, Price = 1000, Status = "PAID" }
        };
        var flightResponse = new FlightClientModel { FlightNumber = TestFlightNumber, FromAirport = "A", ToAirport = "B", Date = TestFlightDate };

        _ticketsClientMock.Setup(c => c.GetAllByUserAsync(TestUsername)).ReturnsAsync(tickets);
        _flightsClientMock.Setup(c => c.GetByFlightNumberAsync(TestFlightNumber, TestUsername)).ReturnsAsync(flightResponse);

        var result = await _controller.GetAll(TestUsername);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<TicketResponse>>(okResult.Value);
        Assert.Single(response);
        Assert.Equal("A", response.First().FromAirport);
        Assert.Equal(TestFlightDate.ToString("yyyy-MM-dd HH:mm"), response.First().Date);
    }

    [Fact]
    public async Task GetAll_SkipsTicket_WhenFlightInfoIsMissing()
    {
        var tickets = new List<TicketClientModel>
        {
            new TicketClientModel { TicketUid = TestTicketUid, FlightNumber = TestFlightNumber, Price = 1000, Status = "PAID" }
        };

        _ticketsClientMock.Setup(c => c.GetAllByUserAsync(TestUsername)).ReturnsAsync(tickets);
        _flightsClientMock.Setup(c => c.GetByFlightNumberAsync(TestFlightNumber, TestUsername)).ReturnsAsync((FlightClientModel?)null);

        var result = await _controller.GetAll(TestUsername);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<TicketResponse>>(okResult.Value);
        Assert.Empty(response);
    }

    [Fact]
    public async Task GetByUid_ReturnsNotFound_WhenTicketMissing()
    {
        _ticketsClientMock.Setup(c => c.GetByUidAsync(TestTicketUid, TestUsername)).ReturnsAsync((TicketClientModel?)null);

        var result = await _controller.GetByUid(TestTicketUid, TestUsername);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var value = (dynamic)notFoundResult.Value!;
        Assert.Contains("Ticket", (string)value.message);
    }

    [Fact]
    public async Task GetByUid_ReturnsNotFound_WhenFlightMissing()
    {
        var ticket = new TicketClientModel { TicketUid = TestTicketUid, FlightNumber = TestFlightNumber, Price = 1000, Status = "PAID" };
        _ticketsClientMock.Setup(c => c.GetByUidAsync(TestTicketUid, TestUsername)).ReturnsAsync(ticket);
        _flightsClientMock.Setup(c => c.GetByFlightNumberAsync(TestFlightNumber, TestUsername)).ReturnsAsync((FlightClientModel?)null);

        var result = await _controller.GetByUid(TestTicketUid, TestUsername);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var value = (dynamic)notFoundResult.Value!;
        Assert.Contains("Flight", (string)value.message);
    }

    [Fact]
    public async Task GetByUid_ReturnsOk_WithFullTicketInfo()
    {
        var ticket = new TicketClientModel { TicketUid = TestTicketUid, FlightNumber = TestFlightNumber, Price = 1000, Status = "PAID" };
        var flightResponse = new FlightClientModel { FlightNumber = TestFlightNumber, FromAirport = "A", ToAirport = "B", Date = TestFlightDate };
        var privilegeResponse = new PrivilegeClientModel { Balance = 500, Status = "SILVER" };

        _ticketsClientMock.Setup(c => c.GetByUidAsync(TestTicketUid, TestUsername)).ReturnsAsync(ticket);
        _flightsClientMock.Setup(c => c.GetByFlightNumberAsync(TestFlightNumber, TestUsername)).ReturnsAsync(flightResponse);
        _bonusClientMock.Setup(c => c.GetPrivilegeAsync(TestUsername)).ReturnsAsync(privilegeResponse);

        var result = await _controller.GetByUid(TestTicketUid, TestUsername);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = (dynamic)okResult.Value!;
        Assert.Equal(TestTicketUid, (Guid)response.ticketUid);
        Assert.Equal("A", (string)response.fromAirport);
    }

    [Fact]
    public async Task Purchase_ReturnsBadRequest_WhenUsernameIsMissing()
    {
        var requestDto = new TicketPurchaseRequest { FlightNumber = TestFlightNumber, Price = 1000, PaidFromBalance = true };

        var result = await _controller.Purchase(null, requestDto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ValidationErrorResponse>(badRequestResult.Value);
    }

    [Theory]
    [InlineData(null, 1000)]
    [InlineData("SU201", 0)]
    public async Task Purchase_ReturnsBadRequest_WhenDtoIsInvalid(string flightNumber, int price)
    {
        var requestDto = new TicketPurchaseRequest { FlightNumber = flightNumber!, Price = price, PaidFromBalance = true };

        var result = await _controller.Purchase(TestUsername, requestDto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ValidationErrorResponse>(badRequestResult.Value);
    }

    [Fact]
    public async Task Purchase_ReturnsNotFound_WhenFlightMissing()
    {
        var requestDto = new TicketPurchaseRequest { FlightNumber = TestFlightNumber, Price = 1000, PaidFromBalance = true };
        _flightsClientMock.Setup(c => c.GetByFlightNumberAsync(TestFlightNumber, TestUsername)).ReturnsAsync((FlightClientModel?)null);

        var result = await _controller.Purchase(TestUsername, requestDto);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.IsType<ErrorResponse>(notFoundResult.Value);
    }

    [Fact]
    public async Task Purchase_ReturnsBadRequest_WhenTicketCreationFails()
    {
        var requestDto = new TicketPurchaseRequest { FlightNumber = TestFlightNumber, Price = 1000, PaidFromBalance = true };
        var flightResponse = new FlightClientModel { FlightNumber = TestFlightNumber, FromAirport = "A", ToAirport = "B", Date = TestFlightDate };

        _flightsClientMock.Setup(c => c.GetByFlightNumberAsync(TestFlightNumber, TestUsername)).ReturnsAsync(flightResponse);
        _ticketsClientMock.Setup(c => c.PurchaseAsync(TestUsername, requestDto)).ReturnsAsync((TicketClientModel?)null);

        var result = await _controller.Purchase(TestUsername, requestDto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ErrorResponse>(badRequestResult.Value);
        _ticketsClientMock.Verify(c => c.CancelAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Purchase_ReturnsBadRequest_AndCancelsTicket_WhenBonusApplyFails()
    {
        var requestDto = new TicketPurchaseRequest { FlightNumber = TestFlightNumber, Price = 1000, PaidFromBalance = true };
        var flightResponse = new FlightClientModel { FlightNumber = TestFlightNumber, FromAirport = "A", ToAirport = "B", Date = TestFlightDate };
        var ticketCreated = new TicketClientModel { TicketUid = TestTicketUid, FlightNumber = TestFlightNumber, Price = 1000, Status = "PAID" };

        _flightsClientMock.Setup(c => c.GetByFlightNumberAsync(TestFlightNumber, TestUsername)).ReturnsAsync(flightResponse);
        _ticketsClientMock.Setup(c => c.PurchaseAsync(TestUsername, requestDto)).ReturnsAsync(ticketCreated);
        _bonusClientMock.Setup(c => c.ApplyAsync(TestUsername, It.IsAny<ApplyBonusRequest>())).ReturnsAsync((BonusApplyResponse?)null);
        _ticketsClientMock.Setup(c => c.CancelAsync(TestTicketUid, TestUsername)).ReturnsAsync(true);

        var result = await _controller.Purchase(TestUsername, requestDto);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ErrorResponse>(badRequestResult.Value);
        _ticketsClientMock.Verify(c => c.CancelAsync(TestTicketUid, TestUsername), Times.Once); 
    }

    [Fact]
    public async Task Purchase_ReturnsOk_OnSuccessfulPurchase()
    {
        var requestDto = new TicketPurchaseRequest { FlightNumber = TestFlightNumber, Price = 1000, PaidFromBalance = true };
        var flightResponse = new FlightClientModel { FlightNumber = TestFlightNumber, FromAirport = "A", ToAirport = "B", Date = TestFlightDate };
        var ticketCreated = new TicketClientModel { TicketUid = TestTicketUid, FlightNumber = TestFlightNumber, Price = 1000, Status = "PAID" };
        var bonusApplyResponse = new BonusApplyResponse { PaidByMoney = 500, PaidByBonuses = 500 };
        var privilegeResponse = new PrivilegeClientModel { Balance = 500, Status = "SILVER" };

        _flightsClientMock.Setup(c => c.GetByFlightNumberAsync(TestFlightNumber, TestUsername)).ReturnsAsync(flightResponse);
        _ticketsClientMock.Setup(c => c.PurchaseAsync(TestUsername, requestDto)).ReturnsAsync(ticketCreated);
        _bonusClientMock.Setup(c => c.ApplyAsync(TestUsername, It.IsAny<ApplyBonusRequest>())).ReturnsAsync(bonusApplyResponse);
        _bonusClientMock.Setup(c => c.GetPrivilegeAsync(TestUsername)).ReturnsAsync(privilegeResponse);

        var result = await _controller.Purchase(TestUsername, requestDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TicketPurchaseResponse>(okResult.Value);
        Assert.Equal(500, response.PaidByMoney);
        Assert.Equal(500, response.PaidByBonuses);
        Assert.Equal("SILVER", response.Privilege!.Status);
        Assert.Equal(TestFlightDate.ToString("yyyy-MM-dd HH:mm"), response.Date);
        _ticketsClientMock.Verify(c => c.CancelAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_ReturnsBadRequest_WhenUsernameIsMissing()
    {
        var result = await _controller.Cancel(TestTicketUid, null);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var value = (dynamic)badRequestResult.Value!;
        Assert.Contains("User header missing", (string)value.message);
    }

    [Fact]
    public async Task Cancel_ReturnsNotFound_WhenTicketMissing()
    {
        _ticketsClientMock.Setup(c => c.GetByUidAsync(TestTicketUid, TestUsername)).ReturnsAsync((TicketClientModel?)null);

        var result = await _controller.Cancel(TestTicketUid, TestUsername);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var value = (dynamic)notFoundResult.Value!;
        Assert.Contains("Ticket", (string)value.message);
    }

    [Fact]
    public async Task Cancel_ReturnsNotFound_WhenCancelFails()
    {
        var ticket = new TicketClientModel { TicketUid = TestTicketUid, FlightNumber = TestFlightNumber, Price = 1000, Status = "PAID" };
        _ticketsClientMock.Setup(c => c.GetByUidAsync(TestTicketUid, TestUsername)).ReturnsAsync(ticket);
        _ticketsClientMock.Setup(c => c.CancelAsync(TestTicketUid, TestUsername)).ReturnsAsync(false);

        var result = await _controller.Cancel(TestTicketUid, TestUsername);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var value = (dynamic)notFoundResult.Value!;
        Assert.Contains("Ticket", (string)value.message);
        _bonusClientMock.Verify(c => c.RefundAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_ReturnsNoContent_OnSuccessfulCancelAndRefund()
    {
        var ticket = new TicketClientModel { TicketUid = TestTicketUid, FlightNumber = TestFlightNumber, Price = 1000, Status = "PAID" };
        _ticketsClientMock.Setup(c => c.GetByUidAsync(TestTicketUid, TestUsername)).ReturnsAsync(ticket);
        _ticketsClientMock.Setup(c => c.CancelAsync(TestTicketUid, TestUsername)).ReturnsAsync(true);
        _bonusClientMock.Setup(c => c.RefundAsync(TestUsername, TestTicketUid)).Returns(Task.CompletedTask);

        var result = await _controller.Cancel(TestTicketUid, TestUsername);

        Assert.IsType<NoContentResult>(result);
        _ticketsClientMock.Verify(c => c.CancelAsync(TestTicketUid, TestUsername), Times.Once);
        _bonusClientMock.Verify(c => c.RefundAsync(TestUsername, TestTicketUid), Times.Once);
    }
}