// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Orleans.Jobs;

using context = Cratis.Chronicle.Kernel.Integration.Events.Constraints.for_ClosedStreamsConstraintStorage.when_reindexing_closing_constraints.context;

namespace Cratis.Chronicle.Kernel.Integration.Events.Constraints.for_ClosedStreamsConstraintStorage;

[Collection(ChronicleCollection.Name)]
public class when_reindexing_closing_constraints(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public ClosedStream[] Closures;
        public bool Started;
        public bool WasOpenBeforeReindex;
        public Concepts.Events.Constraints.ConstraintName Owner { get; } = $"closing-{Guid.NewGuid():N}";

        readonly EventSequenceId _sequence = $"reindex-closures-{Guid.NewGuid():N}";
        readonly EventType _closing = new($"closing-fact-{Guid.NewGuid():N}", EventTypeGeneration.First);
        readonly EventType _reopening = new($"reopening-fact-{Guid.NewGuid():N}", EventTypeGeneration.First);
        IEventSequenceStorage _events;
        IClosedStreamsConstraintStorage _closures;

        async Task Establish()
        {
            var store = Services.GetRequiredService<IStorage>().GetEventStore((Concepts.EventStoreName)Constants.EventStore);
            var ns = store.GetNamespace(Concepts.EventStoreNamespaceName.Default);
            _events = ns.GetEventSequence(_sequence);
            _closures = ns.GetClosedStreamsConstraints(_sequence);
            await store.EventTypes.Register(_closing, new JsonSchema());
            await store.EventTypes.Register(_reopening, new JsonSchema());
            await Append(0, _closing, "still-closed");
            await Append(1, _closing, "reopened");
            await Append(2, _reopening, "reopened");
            await _closures.Close(new(new(EventSourceId: "manual"), ClosedStreamOwner.Manual, EventSequenceNumber.First, null));
            await store.Constraints.SaveDefinition(new Concepts.Events.Constraints.ClosesStreamConstraintDefinition(Owner, [_closing.Id], ClosedStreamDimensions.EventSourceId, [_reopening.Id]) { EventSequences = [_sequence] });
            WasOpenBeforeReindex = !(await _closures.GetForOwner(new(Owner.Value))).Any();
        }

        async Task Because()
        {
            var jobs = GrainFactory.GetJobsManager((Concepts.EventStoreName)Constants.EventStore, Concepts.EventStoreNamespaceName.Default);
            var started = await jobs.Start<IReindexConstraints, ReindexConstraintsRequest>(new(_sequence, [new(Owner, true, [])]));
            Started = started.IsSuccess;
            if (!started.IsSuccess) return;
            await EventStore.Jobs.WaitForThereToBeNoJobs(TimeSpan.FromSeconds(30), Cratis.Chronicle.Jobs.JobStatus.CompletedSuccessfully);
            Closures = [.. await _closures.GetAll()];
        }

        Task Append(EventSequenceNumber number, EventType type, EventSourceId source) => _events.Append(
            number,
            EventSourceType.Default,
            source,
            EventStreamType.All,
            EventStreamId.Default,
            type,
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = new() },
            new Dictionary<EventTypeGeneration, EventHash>());
    }

    [Fact] void should_not_close_history_at_registration() => Context.WasOpenBeforeReindex.ShouldBeTrue();
    [Fact] void should_start_the_reindex_job() => Context.Started.ShouldBeTrue();
    [Fact] void should_rebuild_the_still_closed_source() => Context.Closures.Single(closure => closure.Owner.Value == Context.Owner.Value).Scope.EventSourceId!.Value.ShouldEqual("still-closed");
    [Fact] void should_preserve_the_closing_sequence_number() => Context.Closures.Single(closure => closure.Owner.Value == Context.Owner.Value).SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_keep_the_reopened_source_open() => Context.Closures.Any(closure => closure.Scope.EventSourceId?.Value == "reopened").ShouldBeFalse();
    [Fact] void should_preserve_manual_closures() => Context.Closures.Single(closure => closure.Owner == ClosedStreamOwner.Manual).Scope.EventSourceId!.Value.ShouldEqual("manual");
}
