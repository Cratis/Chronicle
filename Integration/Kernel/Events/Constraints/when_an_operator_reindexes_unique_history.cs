// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using ConstraintsGrain = KernelCore::Cratis.Chronicle.Events.Constraints.IConstraints;
using context = Cratis.Chronicle.Kernel.Integration.Events.Constraints.when_an_operator_reindexes_unique_history.context;
using EventSequenceId = Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;
using JobId = Cratis.Choreography.Jobs.JobId;
using OperatorReindex = Cratis.Chronicle.Events.Constraints.Operator.ReindexConstraints;

namespace Cratis.Chronicle.Kernel.Integration.Events.Constraints;

[Collection(ChronicleCollection.Name)]
public class when_an_operator_reindexes_unique_history(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public JobId Job;
        public bool IndexWasEmpty;
        public bool ClaimWasRebuilt;
        public bool DuplicateRejected;

        readonly EventSequenceId _sequenceId = $"operator-reindex-{Guid.NewGuid():N}";
        readonly EventType _registered = new($"operator-claim-{Guid.NewGuid():N}", EventTypeGeneration.First);
        readonly EventType _unrelated = new($"operator-unregistered-{Guid.NewGuid():N}", EventTypeGeneration.First);
        readonly Concepts.Events.Constraints.ConstraintName _name = $"operator-unique-{Guid.NewGuid():N}";
        Concepts.Events.Constraints.UniqueConstraintDefinition _definition;
        IUniqueConstraintsStorage _index;

        static Task Append(IEventSequenceStorage events, EventSequenceNumber number, EventType type, ExpandoObject content) => events.Append(
            number,
            EventSourceType.Default,
            "owner",
            EventStreamType.All,
            EventStreamId.Default,
            type,
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = content },
            new Dictionary<EventTypeGeneration, EventHash>());

        async Task Establish()
        {
            var eventStore = (Concepts.EventStoreName)Constants.EventStore;
            var store = Services.GetRequiredService<IStorage>().GetEventStore(eventStore);
            await store.EventTypes.Register(_registered, new JsonSchema());
            await store.EventTypes.Register(_unrelated, new JsonSchema());
            _definition = new(_name, [new(_registered.Id, ["email"])]) { EventSequences = [_sequenceId] };
            await GrainFactory.GetGrain<ConstraintsGrain>(eventStore).Register([_definition]);
            await EventStore.Jobs.WaitForThereToBeNoJobs(TimeSpan.FromSeconds(30), Cratis.Chronicle.Jobs.JobStatus.CompletedSuccessfully);
            var ns = store.GetNamespace(Concepts.EventStoreNamespaceName.Default);
            _index = ns.GetUniqueConstraintsStorage(_sequenceId);
            var events = ns.GetEventSequence(_sequenceId);
            var content = new ExpandoObject();
            ((IDictionary<string, object?>)content)["email"] = "reserved@example.com";
            await Append(events, EventSequenceNumber.First, _registered, content);
            await Append(events, 1, _unrelated, new ExpandoObject());
            IndexWasEmpty = !(await _index.GetFor("owner", _definition)).Any();
        }

        async Task Because()
        {
            Job = await new OperatorReindex(Constants.EventStore, Concepts.EventStoreNamespaceName.Default, _sequenceId, [_name.Value]).Handle(GrainFactory, Services.GetRequiredService<KernelCore::Cratis.Chronicle.Events.Constraints.IConstraintIndexes>());
            await EventStore.Jobs.WaitForThereToBeNoJobs(TimeSpan.FromSeconds(30), Cratis.Chronicle.Jobs.JobStatus.CompletedSuccessfully);
            ClaimWasRebuilt = (await _index.GetFor("owner", _definition)).Any();
            var sequence = GrainFactory.GetEventSequence(_sequenceId, (Concepts.EventStoreName)Constants.EventStore, Concepts.EventStoreNamespaceName.Default);
            var duplicate = await sequence.Append(EventSourceType.Default, "challenger", EventStreamType.All, EventStreamId.Default, _registered, new JsonObject { ["email"] = "reserved@example.com" }, CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None);
            DuplicateRejected = duplicate.HasConstraintViolations;
        }
    }

    [Fact] void should_start_the_existing_durable_job() => Context.Job.ShouldNotEqual(JobId.NotSet);
    [Fact] void should_start_with_no_unique_claim() => Context.IndexWasEmpty.ShouldBeTrue();
    [Fact] void should_rebuild_the_historical_claim() => Context.ClaimWasRebuilt.ShouldBeTrue();
    [Fact] void should_reject_a_duplicate_live_append() => Context.DuplicateRejected.ShouldBeTrue();
}
