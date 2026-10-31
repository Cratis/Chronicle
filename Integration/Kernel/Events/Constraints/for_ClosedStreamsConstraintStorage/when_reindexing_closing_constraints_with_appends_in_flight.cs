// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;

using ClosedStream = Cratis.Chronicle.Concepts.Events.Constraints.ClosedStream;
using ClosedStreamDimensions = Cratis.Chronicle.Concepts.Events.Constraints.ClosedStreamDimensions;
using ClosedStreamScope = Cratis.Chronicle.Concepts.Events.Constraints.ClosedStreamScope;
using ConstraintName = Cratis.Chronicle.Concepts.Events.Constraints.ConstraintName;
using context = Cratis.Chronicle.Kernel.Integration.Events.Constraints.for_ClosedStreamsConstraintStorage.when_reindexing_closing_constraints_with_appends_in_flight.context;
using EventSequenceId = Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;

namespace Cratis.Chronicle.Kernel.Integration.Events.Constraints.for_ClosedStreamsConstraintStorage;

[Collection(ChronicleCollection.Name)]
public class when_reindexing_closing_constraints_with_appends_in_flight(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public bool Appended;
        public EventSequenceNumber RestoredPosition;
        public EventSequenceNumber LivePosition;

        readonly EventSequenceId _sequenceId = $"reindex-live-{Guid.NewGuid():N}";
        readonly EventType _closing = new($"closing-live-{Guid.NewGuid():N}", EventTypeGeneration.First);
        readonly ConstraintName _owner = $"closing-live-{Guid.NewGuid():N}";
        readonly ConstraintIndexRebuildRunId _runId = new(Guid.NewGuid());
        readonly ClosedStreamScope _scope = new(EventSourceId: "live-source");
        IClosedStreamsConstraintStorage _closures;
        ClosedStream _historical;

        async Task Establish()
        {
            var eventStore = (Concepts.EventStoreName)Constants.EventStore;
            var store = Services.GetRequiredService<IStorage>().GetEventStore(eventStore);
            var ns = store.GetNamespace(Concepts.EventStoreNamespaceName.Default);
            _closures = ns.GetClosedStreamsConstraints(_sequenceId);
            await store.EventTypes.Register(_closing, new JsonSchema());
            var events = ns.GetEventSequence(_sequenceId);
            await events.Append(EventSequenceNumber.First, EventSourceType.Default, "live-source", EventStreamType.All, EventStreamId.Default, _closing, CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = new() }, new Dictionary<EventTypeGeneration, EventHash>());
            await store.Constraints.SaveDefinition(new Concepts.Events.Constraints.ClosesStreamConstraintDefinition(_owner, [_closing.Id], ClosedStreamDimensions.EventSourceId, [], null) { EventSequences = [_sequenceId] });
            var sequence = GrainFactory.GetEventSequence(_sequenceId, eventStore, Concepts.EventStoreNamespaceName.Default);
            await sequence.RefreshConstraints();
            _historical = new(_scope, _owner.Value, EventSequenceNumber.First, DateTimeOffset.UtcNow);
        }

        async Task Because()
        {
            var sequence = GrainFactory.GetEventSequence(_sequenceId, (Concepts.EventStoreName)Constants.EventStore, Concepts.EventStoreNamespaceName.Default);
            await sequence.ConstraintIndexRebuildStarting(_runId, [_owner]);
            try
            {
                var append = await sequence.Append(EventSourceType.Default, "live-source", EventStreamType.All, EventStreamId.Default, _closing, new JsonObject(), CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None);
                Appended = append.IsSuccess;
                if (!Appended) return;
                LivePosition = (await _closures.GetFor(_owner.Value, _scope))!.SequenceNumber;

                // Force the stale commit at the exact handoff boundary instead of relying on
                // scheduler timing: the rebuild read position zero before the live append.
                await _closures.Close(_historical);
            }
            finally
            {
                await sequence.ConstraintIndexRebuilt(_runId, [_owner], new(true, EventSequenceNumber.First));
            }
            RestoredPosition = (await _closures.GetFor(_owner.Value, _scope))!.SequenceNumber;
        }
    }

    [Fact] void should_admit_the_live_closing_event() => Context.Appended.ShouldBeTrue();
    [Fact] void should_have_appended_after_the_rebuild_position() => Context.LivePosition.ShouldBeGreaterThan(EventSequenceNumber.First);
    [Fact] void should_restore_the_live_closure() => Context.RestoredPosition.ShouldEqual(Context.LivePosition);
}
