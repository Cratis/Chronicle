// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.given;

public class a_closing_constraint_reindex_step : Specification
{
    protected readonly List<AppendedEvent> _events = [];
    protected readonly List<IConstraintDefinition> _definitions = [];
    protected ClosesStreamConstraintDefinition _closing;
    protected Storage.InMemory.Events.Constraints.ClosedStreamsConstraintStorage _rows;
    protected IClosedStreamsConstraintStorage _closures;
    protected ReindexConstraintsStepState _state;
    protected ReindexConstraintsStep _step;
    protected Catch<JobStepResult> _result;

    async Task Establish()
    {
        _closing = new("closing", ["Closed"], ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamId, ["Reopened"], "period");
        _definitions.Add(_closing);
        _rows = new();
        _closures = Substitute.For<IClosedStreamsConstraintStorage>();
        _closures.GetForOwner(Arg.Any<ClosedStreamOwner>()).Returns(call => _rows.GetForOwner(call.Arg<ClosedStreamOwner>()));
        _closures.RemoveAllFor(Arg.Any<ClosedStreamOwner>()).Returns(call => _rows.RemoveAllFor(call.Arg<ClosedStreamOwner>()));
        _closures.Close(Arg.Any<ClosedStream>()).Returns(call => _rows.Close(call.Arg<ClosedStream>()));

        var storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var @namespace = Substitute.For<IEventStoreNamespaceStorage>();
        var eventTypes = Substitute.For<IEventTypesStorage>();
        var sequence = Substitute.For<IEventSequenceStorage>();
        storage.GetEventStore(Arg.Any<Concepts.EventStoreName>()).Returns(eventStore);
        eventStore.GetNamespace(Arg.Any<Concepts.EventStoreNamespaceName>()).Returns(@namespace);
        eventStore.Constraints.GetDefinitions().Returns(_ => _definitions.ToArray());
        eventStore.EventTypes.Returns(eventTypes);
        @namespace.GetEventSequence(EventSequenceId.Log).Returns(sequence);
        @namespace.GetClosedStreamsConstraints(EventSequenceId.Log).Returns(_closures);
        @namespace.GetUniqueConstraintsStorage(EventSequenceId.Log).Returns(Substitute.For<IUniqueConstraintsStorage>());
        eventTypes.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(call =>
            new EventTypeSchema(new EventType(call.Arg<EventTypeId>(), EventTypeGeneration.First), EventTypeOwner.Server, EventTypeSource.Code, new JsonSchema()));
        sequence.GetFromSequenceNumber(EventSequenceNumber.First, cancellationToken: Arg.Any<CancellationToken>()).Returns(_ => new EventCursor(_events));

        var silo = new TestKitSilo();
        silo.AddService(storage);
        silo.AddService(Substitute.For<IJsonSchemaMetadataManager>());
        silo.AddService(Substitute.For<IExpandoObjectConverter>());
        silo.AddService(Substitute.For<IJobStepThrottle>());
        silo.AddService(NullLogger<ReindexConstraintsStep>.Instance);
        silo.AddPersistentStateStorage<ReindexConstraintsStepState>(nameof(ReindexConstraintsStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps);
        _step = await silo.CreateGrainAsync<ReindexConstraintsStep>(JobStepId.New(), new JobStepKey(JobId.New(), "store", "ns"));
        _state = new() { EventSequenceId = EventSequenceId.Log, Changes = [new("closing", true, [ConstraintChangeType.EventAdded])] };
    }

    protected async Task Perform() => _result = await (Task<Catch<JobStepResult>>)typeof(ReindexConstraintsStep)
        .GetMethod("PerformStep", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_step, [_state, CancellationToken.None])!;

    protected static ExpandoObject Payload(string property, object value)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)[property] = value;
        return content;
    }

    protected static AppendedEvent Event(EventTypeId type, ulong sequenceNumber, ExpandoObject? content = null) => new(
        EventContext.From("store", "ns", new EventType(type, EventTypeGeneration.First), EventSourceType.Default, "source", "transactions", "month", new EventSequenceNumber(sequenceNumber), CorrelationId.NotSet),
        content ?? new ExpandoObject());
}
