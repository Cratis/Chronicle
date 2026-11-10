// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Appends durable intentions with idempotent retry after uncertain outcomes.
/// </summary>
internal static class Producer
{
    /// <summary>
    /// Produces contributions until cancellation.
    /// </summary>
    /// <param name="store">The isolated event store.</param>
    /// <param name="ledger">The append intention ledger.</param>
    /// <param name="cancellation">The workload cancellation token.</param>
    /// <returns>The producer lifetime task.</returns>
    public static async Task Run(IEventStore store, Ledger ledger, CancellationToken cancellation)
    {
        var index = 0;
        while (!cancellation.IsCancellationRequested)
        {
            var pending = await ledger.Pending();
            var id = pending?.Entry.Id ?? Guid.NewGuid().ToString("N");
            var source = pending?.Entry.Source ?? $"source-{index++ % 4}";
            var @event = pending?.Event ?? new ChaosEvent(id, DateTimeOffset.UtcNow, (index % 97) + 1);
            if (pending is null) await ledger.Intend(id, source, @event);
            await Attempt(store, ledger, source, @event, cancellation);
            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellation);
        }
    }

    /// <summary>
    /// Classifies definitive refusals separately from ambiguous append failures.
    /// </summary>
    /// <param name="result">The append response.</param>
    /// <returns>The durable ledger outcome.</returns>
    internal static string StatusFor(AppendResult result)
    {
        if (result.HasConstraintViolations || result.HasConcurrencyViolations || result.HasIdempotencyConflicts) return "failed";
        return result.IsSuccess ? "acked" : "unknown";
    }

    static async Task Attempt(IEventStore store, Ledger ledger, string source, ChaosEvent @event, CancellationToken cancellation)
    {
        try
        {
            var result = await store.EventLog.AppendOnce(source, @event, new IdempotencyKey(@event.LedgerId)).WaitAsync(TimeSpan.FromSeconds(10), cancellation);
            await ledger.Outcome(@event.LedgerId, StatusFor(result));
        }
        catch (Exception exception) when (!cancellation.IsCancellationRequested && exception is not InvalidOperationException)
        {
            // Transport exceptions, including timeouts, do not establish that the kernel rejected the append.
            await Console.Error.WriteLineAsync($"Append outcome unknown: {exception.GetType().Name}");
            await ledger.Outcome(@event.LedgerId, "unknown");
        }
    }
}
