using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using TicketService;

public class TicketsControllerTests
{
    private readonly Mock<ITicketRepo> _repoMock;
    private readonly TicketsController _controller;
    private const string TestUsername = "test_user";
    private readonly Guid TestTicketUid = new Guid("4b14d59a-2423-4555-8938-1a5c60959828");

    public TicketsControllerTests()
    {
        _repoMock = new Mock<ITicketRepo>();
        _controller = new TicketsController(_repoMock.Object);
    }

    // --- GetAll Tests ---

    [Fact]
    public async Task GetAll_ReturnsOk_WithTicketsList()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            new Ticket { TicketUid = Guid.NewGuid(), FlightNumber = "SU201", Price = 1000, Status = "PAID" },
            new Ticket { TicketUid = Guid.NewGuid(), FlightNumber = "UT501", Price = 2000, Status = "PAID" }
        };
        _repoMock.Setup(r => r.GetAllByUserAsync(TestUsername)).ReturnsAsync(tickets);

        // Act
        var result = await _controller.GetAll(TestUsername);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<TicketResponse>>(okResult.Value);
        Assert.Equal(2, response.Count());
        _repoMock.Verify(r => r.GetAllByUserAsync(TestUsername), Times.Once);
    }

    [Fact]
    public async Task GetAll_ReturnsBadRequest_WhenUsernameIsMissing()
    {
        // Act
        var result = await _controller.GetAll(null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<ValidationErrorResponse>(badRequestResult.Value);
        Assert.Contains("User header missing", errorResponse.Message);
        Assert.True(errorResponse.Errors.ContainsKey("X-User-Name"));
    }

    // --- GetByUid Tests ---

    [Fact]
    public async Task GetByUid_ReturnsOk_WhenTicketExists()
    {
        // Arrange
        var ticket = new Ticket{
            TicketUid = TestTicketUid, 
            Username = TestUsername, 
            FlightNumber = "SU201", 
            Price = 1000, 
            Status = "PAID"
        };
        _repoMock.Setup(r => r.GetByUidAsync(TestTicketUid, TestUsername)).ReturnsAsync(ticket);

        // Act
        var result = await _controller.GetByUid(TestTicketUid, TestUsername);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TicketResponse>(okResult.Value);
        Assert.Equal(TestTicketUid, response.TicketUid);
    }

    [Fact]
    public async Task GetByUid_ReturnsNotFound_WhenTicketMissing()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByUidAsync(TestTicketUid, TestUsername)).ReturnsAsync((Ticket?)null);

        // Act
        var result = await _controller.GetByUid(TestTicketUid, TestUsername);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Contains("not found", errorResponse.Message);
    }

    [Fact]
    public async Task GetByUid_ReturnsBadRequest_WhenUsernameIsMissing()
    {
        // Act
        var result = await _controller.GetByUid(TestTicketUid, null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<ValidationErrorResponse>(badRequestResult.Value);
        Assert.Contains("User header missing", errorResponse.Message);
    }

    // --- Purchase Tests ---

    [Fact]
    public async Task Purchase_ReturnsBadRequest_WhenUsernameIsMissing()
    {
        // Arrange
        var requestDto = new TicketPurchaseRequest { FlightNumber = "SU201", Price = 1000, PaidFromBalance = false };

        // Act
        var result = await _controller.Purchase(null, requestDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<ValidationErrorResponse>(badRequestResult.Value);
        Assert.Contains("User header missing", errorResponse.Message);
    }

    [Fact]
    public async Task Purchase_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        var requestDto = new TicketPurchaseRequest { FlightNumber = "", Price = 0, PaidFromBalance = false };
        _controller.ModelState.AddModelError("FlightNumber", "Required");
        _controller.ModelState.AddModelError("Price", "Must be greater than 0");

        // Act
        var result = await _controller.Purchase(TestUsername, requestDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<ValidationErrorResponse>(badRequestResult.Value);
        Assert.Contains("Invalid data", errorResponse.Message);
        Assert.Contains("FlightNumber", errorResponse.Errors.Keys);
    }

    // --- Cancel Tests ---

    [Fact]
    public async Task Cancel_ReturnsNoContent_WhenTicketExists()
    {
        // Arrange
        var ticket = new Ticket{
            TicketUid = TestTicketUid, 
            Username = TestUsername, 
            FlightNumber = "SU201", 
            Price = 1000, 
            Status = "PAID"
        };        _repoMock.Setup(r => r.GetByUidAsync(TestTicketUid, TestUsername)).ReturnsAsync(ticket);
        _repoMock.Setup(r => r.UpdateAsync(It.Is<Ticket>(t => t.Status == "CANCELED"))).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Cancel(TestTicketUid, TestUsername);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Ticket>(t => t.Status == "CANCELED")), Times.Once);
    }

    [Fact]
    public async Task Cancel_ReturnsNotFound_WhenTicketMissing()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByUidAsync(TestTicketUid, TestUsername)).ReturnsAsync((Ticket?)null);

        // Act
        var result = await _controller.Cancel(TestTicketUid, TestUsername);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Contains("not found", errorResponse.Message);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Ticket>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_ReturnsBadRequest_WhenUsernameIsMissing()
    {
        // Act
        var result = await _controller.Cancel(TestTicketUid, null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<ValidationErrorResponse>(badRequestResult.Value);
        Assert.Contains("User header missing", errorResponse.Message);
    }
}