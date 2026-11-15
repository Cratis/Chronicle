// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.Options;
using StartupOccurrence = Cratis.Chronicle.Occurrences.Startup;

namespace Cratis.Chronicle.Integration.for_PerProcessActivation;

public sealed class PerProcessStartupHandler(IChronicleClient client, IOptions<ChronicleClientOptions> options) : IOccurrenceHandler<StartupOccurrence>
{
    public async Task Handle(StartupOccurrence trigger, OccurrenceContext context, CancellationToken cancellationToken)
    {
        var store = await client.GetEventStore(options.Value.EventStore, context.Namespace ?? EventStoreNamespaceName.Default);
        using var unit = new UnitOfWorkManager(store).Begin(CorrelationId.New());
        unit.UseEffectReceipt(context.EffectKey("handled"));
        unit.AddEvent(EventSequenceId.Log, context.Id.Value, new ProcessStarted(Environment.ProcessId), Causation.Unknown());
        await unit.Commit();
        unit.IsSuccess.ShouldBeTrue();
        await Console.Out.WriteLineAsync($"STARTED {context.Id.Value} {Environment.ProcessId}");
        await Console.Out.FlushAsync(cancellationToken);
    }
}
