```csharp
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public record SelfReplayBalance(decimal Balance)
{
    public static SelfReplayBalance Reduce(SelfBalanceAdded @event, SelfReplayBalance? current) =>
        new((current?.Balance ?? 0m) + @event.Amount);

    public static Task BeginReplay(BalanceReplayAudit audit) => audit.Write("begin");
    public static Task EndReplay(BalanceReplayAudit audit) => audit.Write("end");
    public static Task BeginReplayPartition(Partition partition, BalanceReplayAudit audit) =>
        audit.Write($"begin partition {partition.Value}");
    public static Task EndReplayPartition(Partition partition, BalanceReplayAudit audit) =>
        audit.Write($"end partition {partition.Value}");
}

public sealed class BalanceReplayAudit(ILogger<BalanceReplayAudit> logger) : IAsyncDisposable
{
    public Task Write(string phase)
    {
        logger.LogInformation("Replay phase: {Phase}", phase);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        logger.LogInformation("Replay notification scope disposed");
        return ValueTask.CompletedTask;
    }
}

public static class BalanceReplayServices
{
    public static void Configure(IServiceCollection services) => services.AddScoped<BalanceReplayAudit>();
}
```
