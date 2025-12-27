using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Text.Json;
using BonusService;

public class BonusControllerTests
{
    private readonly Mock<IBonusRepo> _repoMock;
    private readonly BonusController _controller;
    private const string TestUsername = "test_user";
    private readonly Guid TestTicketUid = new Guid("4b14d59a-2423-4555-8938-1a5c60959828");

    public BonusControllerTests()
    {
        _repoMock = new Mock<IBonusRepo>();
        _controller = new BonusController(_repoMock.Object);
    }


    [Fact]
    public async Task Get_ReturnsOk_WhenPrivilegeExists()
    {
        var privilege = new Privilege { Username = TestUsername, Balance = 150, Status = "SILVER" };
        _repoMock.Setup(r => r.GetByUsernameAsync(TestUsername)).ReturnsAsync(privilege);

        var result = await _controller.Get(TestUsername);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PrivilegeResponse>(okResult.Value);
        Assert.Equal(150, response.Balance);
        _repoMock.Verify(r => r.AddPrivilegeAsync(It.IsAny<Privilege>()), Times.Never);
    }

    [Fact]
    public async Task Get_CreatesNewPrivilege_WhenMissing()
    {
        _repoMock.Setup(r => r.GetByUsernameAsync(TestUsername)).ReturnsAsync((Privilege?)null);
        
        var result = await _controller.Get(TestUsername);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PrivilegeResponse>(okResult.Value);
        Assert.Equal(0, response.Balance);
        Assert.Equal("BRONZE", response.Status);
        
        _repoMock.Verify(r => r.AddPrivilegeAsync(It.Is<Privilege>(p => p.Username == TestUsername && p.Balance == 0)), Times.Once);
    }

    [Fact]
    public async Task Get_ReturnsBadRequest_WhenUsernameIsMissing()
    {
        var result = await _controller.Get(null);

        Assert.IsType<BadRequestObjectResult>(result);
    }


    [Fact]
    public async Task ApplyBonus_PaidFromBalance_DebitsBonus_AndReturnsCorrectPaidAmounts()
    {
        var initialBalance = 200;
        var reqPrice = 150;
        var privilege = new Privilege { Id = 1, Username = TestUsername, Balance = initialBalance, Status = "BRONZE" };
        var requestDto = new ApplyBonusRequest { TicketUid = TestTicketUid, Price = reqPrice, PaidFromBalance = true };
        
        _repoMock.Setup(r => r.GetByUsernameAsync(TestUsername)).ReturnsAsync(privilege);
        
        var result = await _controller.ApplyBonus(TestUsername, requestDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value as dynamic;

        Assert.Equal(50, privilege.Balance);

        _repoMock.Verify(r => r.AddHistoryAsync(It.Is<PrivilegeHistory>(
            h => h.OperationType == "DEBIT_THE_ACCOUNT" && h.BalanceDiff == -150
        )), Times.Once);
        _repoMock.Verify(r => r.UpdatePrivilegeAsync(privilege), Times.Once);
    }

    [Fact]
    public async Task ApplyBonus_PaidFromBalance_UsesPartialBonus()
    {
        var initialBalance = 50;
        var reqPrice = 150;
        var privilege = new Privilege { Id = 1, Username = TestUsername, Balance = initialBalance, Status = "BRONZE" };
        var requestDto = new ApplyBonusRequest { TicketUid = TestTicketUid, Price = reqPrice, PaidFromBalance = true };
        
        _repoMock.Setup(r => r.GetByUsernameAsync(TestUsername)).ReturnsAsync(privilege);
        
        var result = await _controller.ApplyBonus(TestUsername, requestDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value as dynamic;

        Assert.Equal(0, privilege.Balance);
    }

    [Fact]
    public async Task ApplyBonus_NotPaidFromBalance_FillsInBonus()
    {
        var initialBalance = 100;
        var reqPrice = 1000;
        var expectedBonus = 100;
        var privilege = new Privilege { Id = 1, Username = TestUsername, Balance = initialBalance, Status = "BRONZE" };
        var requestDto = new ApplyBonusRequest { TicketUid = TestTicketUid, Price = reqPrice, PaidFromBalance = false };
        
        _repoMock.Setup(r => r.GetByUsernameAsync(TestUsername)).ReturnsAsync(privilege);
        
        var result = await _controller.ApplyBonus(TestUsername, requestDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value as dynamic;
        
        Assert.Equal(initialBalance + expectedBonus, privilege.Balance);

        _repoMock.Verify(r => r.AddHistoryAsync(It.Is<PrivilegeHistory>(
            h => h.OperationType == "FILL_IN_BALANCE" && h.BalanceDiff == expectedBonus
        )), Times.Once);
        _repoMock.Verify(r => r.UpdatePrivilegeAsync(privilege), Times.Once);
    }

    [Theory]
    [InlineData(4999, 10, "SILVER")]
    [InlineData(9990, 100, "GOLD")]
    [InlineData(500, 0, "BRONZE")]
    public async Task ApplyBonus_UpdatesStatusCorrectly(int initialBalance, int price, string expectedStatus)
    {
        var privilege = new Privilege { Id = 1, Username = TestUsername, Balance = initialBalance, Status = "BRONZE" };
        var requestDto = new ApplyBonusRequest { TicketUid = TestTicketUid, Price = price, PaidFromBalance = false };
        
        int bonus = (int)(price * 0.1);
        int expectedBalance = initialBalance + bonus;
        
        _repoMock.Setup(r => r.GetByUsernameAsync(TestUsername)).ReturnsAsync(privilege);
        
        await _controller.ApplyBonus(TestUsername, requestDto);

        Assert.Equal(expectedBalance, privilege.Balance);
        Assert.Equal(expectedStatus, privilege.Status);
        _repoMock.Verify(r => r.UpdatePrivilegeAsync(privilege), Times.Once);
    }

    [Fact]
    public async Task Refund_ReturnsNoContent_AndDebitsBalance_OnFillInReversal()
    {
        var initialBalance = 200;
        var gainedBonus = 100;
        var privilege = new Privilege { Id = 1, Username = TestUsername, Balance = initialBalance, Status = "BRONZE" };
        var lastHistory = new PrivilegeHistory
        {
            Privilege = privilege,
            TicketUid = TestTicketUid,
            BalanceDiff = gainedBonus,
            OperationType = "FILL_IN_BALANCE" 
        };

        _repoMock.Setup(r => r.GetLastHistoryByTicketAsync(TestTicketUid)).ReturnsAsync(lastHistory);

        var result = await _controller.Refund(TestUsername, TestTicketUid);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(initialBalance - gainedBonus, privilege.Balance);

        _repoMock.Verify(r => r.AddHistoryAsync(It.Is<PrivilegeHistory>(
            h => h.OperationType == "DEBIT_THE_ACCOUNT" && h.BalanceDiff == 100
        )), Times.Once);
        _repoMock.Verify(r => r.UpdatePrivilegeAsync(privilege), Times.Once);
    }

    [Fact]
    public async Task Refund_ReturnsNotFound_WhenNoHistoryFound()
    {
        _repoMock.Setup(r => r.GetLastHistoryByTicketAsync(TestTicketUid)).ReturnsAsync((PrivilegeHistory?)null);

        var result = await _controller.Refund(TestUsername, TestTicketUid);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Refund_ReturnsBadRequest_WhenUsernameIsMissing()
    {
        var result = await _controller.Refund(null, TestTicketUid);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}