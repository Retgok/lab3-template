using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using FlightService; 

public class FlightsControllerTests
{
    private readonly Mock<IFlightRepo> _repoMock;
    private readonly FlightsController _controller;
    private const string TestFlightNumber = "SU201";
    private const string TestUsername = "test_user";

    private readonly Airport TestAirportA = new Airport { Id = 1, Name = "Sheremetyevo", City = "Moscow", Country = "Russia" };
    private readonly Airport TestAirportB = new Airport { Id = 2, Name = "Pulkovo", City = "St. Petersburg", Country = "Russia" };

    public FlightsControllerTests()
    {
        _repoMock = new Mock<IFlightRepo>();
        _controller = new FlightsController(_repoMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithFlightsListAndFormattedAirports()
    {
        var flights = new List<Flight>
        {
            new Flight { FlightNumber = TestFlightNumber, Price = 1000, FromAirport = TestAirportA, ToAirport = TestAirportB, DateTime = DateTime.Now },
            new Flight { FlightNumber = "UT501", Price = 2000, FromAirport = TestAirportB, ToAirport = TestAirportA, DateTime = DateTime.Now }
        };
        _repoMock.Setup(r => r.GetAllAsync(0, 100)).ReturnsAsync(flights);

        var result = await _controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<FlightResponse>>(okResult.Value).ToList();
        
        Assert.Equal(2, response.Count);
        
        Assert.Equal("Moscow Sheremetyevo", response[0].FromAirport);
        Assert.Equal("St. Petersburg Pulkovo", response[0].ToAirport);
        
        _repoMock.Verify(r => r.GetAllAsync(0, 100), Times.Once);
    }

    [Fact]
    public async Task GetAll_UsesCorrectPaging()
    {
        _repoMock.Setup(r => r.GetAllAsync(1, 50)).ReturnsAsync(new List<Flight>());

        var result = await _controller.GetAll(page: 1, size: 50);

        Assert.IsType<OkObjectResult>(result);
        _repoMock.Verify(r => r.GetAllAsync(1, 50), Times.Once);
    }

    [Fact]
    public async Task GetByFlightNumber_ReturnsOk_WhenFlightExistsAndFormatsAirport()
    {
        var flight = new Flight 
        { 
            FlightNumber = TestFlightNumber, 
            Price = 1000, 
            FromAirport = TestAirportA, 
            ToAirport = TestAirportB, 
            DateTime = new DateTime(2025, 12, 13) 
        };
        _repoMock.Setup(r => r.GetByFlightNumberAsync(TestFlightNumber)).ReturnsAsync(flight);

        var result = await _controller.GetByFlightNumber(TestFlightNumber, TestUsername);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<FlightResponse>(okResult.Value);
        
        Assert.Equal(TestFlightNumber, response.FlightNumber);
        Assert.Equal("Moscow Sheremetyevo", response.FromAirport);
        Assert.Equal("St. Petersburg Pulkovo", response.ToAirport);
        Assert.Equal(new DateTime(2025, 12, 13), response.DateTime);
        
        _repoMock.Verify(r => r.GetByFlightNumberAsync(TestFlightNumber), Times.Once);
    }

    [Fact]
    public async Task GetByFlightNumber_ReturnsNotFound_WhenFlightMissing()
    {
        _repoMock.Setup(r => r.GetByFlightNumberAsync(TestFlightNumber)).ReturnsAsync((Flight?)null);

        var result = await _controller.GetByFlightNumber(TestFlightNumber, TestUsername);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Contains("not found", errorResponse.Message);
    }

    [Fact]
    public async Task GetByFlightNumber_ReturnsBadRequest_WhenUsernameIsMissing()
    {
        var result = await _controller.GetByFlightNumber(TestFlightNumber, null);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = Assert.IsType<ValidationErrorResponse>(badRequestResult.Value);
        Assert.Contains("User header missing", errorResponse.Message);
    }
}