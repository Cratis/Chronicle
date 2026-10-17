// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.Occurrences.for_OccurrenceScenario.given;

public class a_scenario : Specification
{
    protected OccurrenceScenario<Handler> _scenario;
    protected OccurrenceRecordResult _recorded;
    protected Defaults _defaults;

    async Task Establish()
    {
        _defaults = new(new InMemoryClientArtifactsProvider { EventTypes = [typeof(EffectWasRecorded)], Triggers = [typeof(ImportRequested)] });
        _scenario = new(_defaults);
        _recorded = await _scenario.Record(new ImportRequested("one"), "request-one");
    }

    void Destroy()
    {
        _scenario.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }

    [Trigger("testing-import")]
    public record ImportRequested(string Name);

    [EventType]
    public record EffectWasRecorded(string Name);

    public sealed class Handler(IEventStore store) : IOccurrenceHandler<ImportRequested>
    {
        public async Task Handle(ImportRequested trigger, OccurrenceContext context, CancellationToken cancellationToken)
        {
            using var unit = new UnitOfWork(CorrelationId.New(), _ => { }, store);
            unit.UseEffectReceipt(context.EffectKey("import"));
            unit.AddEvent(EventSequenceId.Log, "import", new EffectWasRecorded(trigger.Name), Causation.Unknown());
            await unit.Commit();
        }
    }
}
