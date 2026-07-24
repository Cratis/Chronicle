// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Metrics;
using Cratis.Traces;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Core;
using Orleans.TestKit;
using IChronicleStorage = Cratis.Chronicle.Storage.IStorage;
using JsonExpandoObjectConverter = Cratis.Chronicle.Json.IExpandoObjectConverter;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence : Specification
{
    protected TestKitSilo _silo;
    protected EventSequence _grain;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected IStorage<EventSequenceState> _stateStorage;
    protected EventSequenceKey _key;

    async Task Establish()
    {
        _silo = new();
        _key = new EventSequenceKey(EventSequenceId.Log, "TestStore", "TestNamespace");

        var storage = Substitute.For<IChronicleStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();

        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);
        eventStoreStorage.EventTypes.Returns(Substitute.For<IEventTypesStorage>());
        namespaceStorage.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequenceStorage);
        namespaceStorage.Identities.Returns(Substitute.For<IIdentityStorage>());

        var constraintValidationFactory = Substitute.For<IConstraintValidationFactory>();
        constraintValidationFactory.Create(Arg.Any<EventSequenceKey>()).Returns(Substitute.For<IConstraintValidation>());

        var eventHashCalculator = Substitute.For<IEventHashCalculator>();
        eventHashCalculator.Calculate(Arg.Any<EventTypeId>(), Arg.Any<EventSourceId>(), Arg.Any<ExpandoObject>()).Returns(EventHash.NotSet);

        _silo.AddService(storage);
        _silo.AddService(constraintValidationFactory);
        _silo.AddService(Substitute.For<IEventTypeMigrations>());
        _silo.AddService(Substitute.For<IJsonComplianceManager>());
        _silo.AddService(Substitute.For<JsonExpandoObjectConverter>());
        _silo.AddService(Substitute.For<IEventSerializer>());
        _silo.AddService(eventHashCalculator);
        _silo.AddService(NullLogger<EventSequence>.Instance);
        _silo.AddKeyedService<IMeter<EventSequence>>(WellKnown.MeterName, Substitute.For<IMeter<EventSequence>>());
        _silo.AddKeyedService<IActivitySource<EventSequence>>(WellKnown.MeterName, new ActivitySource<EventSequence>());

        _silo.AddProbe(_ => Substitute.For<INamespaces>());
        _silo.AddProbe(_ => Substitute.For<IAppendedEventsQueues>());

        _stateStorage = _silo.StorageManager.GetStorage<EventSequenceState>(typeof(EventSequence).FullName!);
        _stateStorage.State = new EventSequenceState();

        _grain = await _silo.CreateGrainAsync<EventSequence>(_key.ToString());
    }

    /// <summary>
    /// Build a single already validated and compliant event ready to be appended to storage, bypassing the
    /// schema/compliance/constraint pipeline so specs can exercise the batch append and sequence-number logic directly.
    /// </summary>
    /// <returns>A validated event tuple in the shape expected by <see cref="EventSequence.AppendManyToStorage"/>.</returns>
    protected static (EventToAppend Event, ExpandoObject CompliantEvent, ConstraintValidationContext ConstraintContext) ValidatedEvent()
    {
        var eventSourceId = EventSourceId.New();
        var eventType = new EventType("BatchEvent", EventTypeGeneration.First);
        var content = new ExpandoObject();
        var eventToAppend = new EventToAppend(
            EventSourceType.Default,
            eventSourceId,
            EventStreamType.All,
            new EventStreamId(EventStreamId.Default),
            eventType,
            [],
            new JsonObject());
        var constraintContext = new ConstraintValidationContext([], eventSourceId, eventType.Id, content);
        return (eventToAppend, content, constraintContext);
    }
}
