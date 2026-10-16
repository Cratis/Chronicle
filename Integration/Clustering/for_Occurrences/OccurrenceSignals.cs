// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

public sealed class OccurrenceSignals
{
    readonly ConcurrentDictionary<string, int> _effects = new(StringComparer.Ordinal);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _paused;

    public bool PauseFirstWork { get; set; }
    public TaskCompletionSource<OccurrenceContext> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Effects(string id) => _effects.GetValueOrDefault(id);

    public async Task Settle(IChronicleClient client, OccurrenceContext context, CancellationToken cancellationToken)
    {
        var store = await client.GetEventStore(Constants.EventStore, context.Namespace ?? EventStoreNamespaceName.Default);
        using var unit = new UnitOfWorkManager(store).Begin(CorrelationId.New());
        unit.UseEffectReceipt(context.EffectKey("handled"));
        await store.EventLog.Append(context.Id.Value, new OccurrenceWorkCompleted("handled"));
        await unit.Commit();
        unit.IsSuccess.ShouldBeTrue();
        if (unit.EffectReceipt!.Status == EffectReceiptStatus.Recorded) _effects.AddOrUpdate(context.Id.Value, 1, (_, count) => count + 1);

        // Kill after the effect commit but before returning to the delivery loop's Complete call.
        if (context.Trigger.Value == "p3c-work" && PauseFirstWork && Interlocked.CompareExchange(ref _paused, 1, 0) == 0)
        {
            Entered.TrySetResult(context);
            await _release.Task.WaitAsync(cancellationToken);
        }
    }
}
