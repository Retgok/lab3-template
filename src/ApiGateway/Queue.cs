using Microsoft.Extensions.Hosting;
using System.Threading.Channels;
using System;

namespace ApiGatewayService;


public record BonusRefundJob(
    string Username,
    Guid TicketUid
);

public enum RefundResult
{
    Success,
    NotNeeded,
    Retry
}


public interface IBonusRefundQueue
{
    ValueTask EnqueueAsync(BonusRefundJob job);
    ChannelReader<BonusRefundJob> Reader { get; }
}

public class BonusRefundQueue : IBonusRefundQueue
{
    private readonly Channel<BonusRefundJob> _channel =
        Channel.CreateUnbounded<BonusRefundJob>();

    public ValueTask EnqueueAsync(BonusRefundJob job)
        => _channel.Writer.WriteAsync(job);

    public ChannelReader<BonusRefundJob> Reader => _channel.Reader;
}


public class BonusRefundWorker : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    private readonly IBonusRefundQueue _queue;
    private readonly BonusClient _bonus;
    private readonly ILogger<BonusRefundWorker> _logger;

    public BonusRefundWorker(
        IBonusRefundQueue queue,
        BonusClient bonus,
        ILogger<BonusRefundWorker> logger)
    {
        _queue = queue;
        _bonus = bonus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessJobAsync(job, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(
        BonusRefundJob job,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _bonus.RefundAsync(job.Username, job.TicketUid);

                if (result is RefundResult.Success or RefundResult.NotNeeded)
                {
                    Console.WriteLine(
                        "Bonus refund completed. Ticket={0}, Result={1}",
                        job.TicketUid, result);

                    return;
                }

                Console.WriteLine(
                    "Bonus refund retry required. Ticket={0}",
                    job.TicketUid);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Bonus refund failed. Ticket={0}",
                    job.TicketUid);
            }

            await Task.Delay(RetryDelay, stoppingToken);
        }
    }
}
