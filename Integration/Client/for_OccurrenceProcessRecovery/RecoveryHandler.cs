// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Integration.for_OccurrenceProcessRecovery;

public sealed class RecoveryHandler(IChronicleClient client, IOptions<ChronicleClientOptions> options, PostCommitBarrier barrier) : IOccurrenceHandler<RecoveryWork>
{
    public async Task Handle(RecoveryWork trigger, OccurrenceContext context, CancellationToken cancellationToken)
    {
        var store = await client.GetEventStore(options.Value.EventStore, context.Namespace ?? EventStoreNamespaceName.Default).WaitAsync(TimeSpan.FromSeconds(60));
        using var unit = new UnitOfWorkManager(store).Begin(CorrelationId.New());
        unit.UseEffectReceipt(context.EffectKey("handled"));
        unit.AddEvent(EventSequenceId.Log, context.Id.Value, new RecoveryEffect(Environment.ProcessId), Causation.Unknown());
        await unit.Commit().WaitAsync(TimeSpan.FromSeconds(60));
        unit.IsSuccess.ShouldBeTrue();
        var result = unit.EffectReceipt!;
        await barrier.AfterCommit(string.Create(CultureInfo.InvariantCulture,
            $"SETTLED {context.Id.Value} {context.Lease.Epoch} {result.Status} {result.Receipt.WrittenBy.Epoch} {Environment.ProcessId}"));
    }
}
