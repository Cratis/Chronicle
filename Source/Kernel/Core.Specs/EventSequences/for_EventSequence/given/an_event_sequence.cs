// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Metrics;
using Cratis.Monads;
using Cratis.Traces;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence : Specification
{
    protected static readonly EventStoreName EventStore = "test-store";
    protected static readonly EventStoreNamespaceName EventStoreNamespace = "test-namespace";

    protected TestKitSilo _silo = new();
    protected EventSequence _eventSequence;
    protected EventSequenceKey _eventSequenceKey;
    protected EventSourceId _eventSourceId;
    protected EventType _eventType;

    protected List<EventSequenceNumber> _constraintIndexSequenceNumbers;
    protected EventSequenceNumber _appendedSequenceNumber;

    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected IEventTypesStorage _eventTypesStorage;
    protected IIdentityStorage _identityStorage;
    protected IConstraintValidationFactory _constraintValidationFactory;
    protected IConstraintValidation _constraintValidation;
    protected IConstraintValidation _currentValidation;
    protected IEventTypeMigrations _eventTypeMigrations;
    protected IJsonComplianceManager _complianceManager;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected RecordingConstraintValidator _recordingValidator;
    protected INamespaces _namespaces;
    protected IAppendedEventsQueues _appendedEventsQueues;
    protected IConstraints _constraintsGrain;
    protected IJobsManager _jobsManager;
    protected List<IConstraintDefinition> _registeredConstraints;

    async Task Establish()
    {
        _eventSequenceKey = new EventSequenceKey(EventSequenceId.Log, EventStore, EventStoreNamespace);
        _eventSourceId = "some-event-source";
        _eventType = new EventType("some-event", EventTypeGeneration.First);
        _appendedSequenceNumber = EventSequenceNumber.Unavailable;
        _constraintIndexSequenceNumbers = [];
        _recordingValidator = new RecordingConstraintValidator(_constraintIndexSequenceNumbers);

        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _identityStorage = Substitute.For<IIdentityStorage>();
        _constraintValidationFactory = Substitute.For<IConstraintValidationFactory>();
        _constraintValidation = Substitute.For<IConstraintValidation>();
        _eventTypeMigrations = Substitute.For<IEventTypeMigrations>();
        _complianceManager = Substitute.For<IJsonComplianceManager>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _eventStoreStorage.EventTypes.Returns(_eventTypesStorage);
        _namespaceStorage.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequenceStorage);
        _namespaceStorage.Identities.Returns(_identityStorage);

        _eventTypesStorage.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>())
            .Returns(new EventTypeSchema(_eventType, EventTypeOwner.Server, EventTypeSource.Code, new JsonSchema()));

        _identityStorage.GetFor(Arg.Any<Identity>()).Returns(ImmutableList<IdentityId>.Empty);

        _complianceManager.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(_ => Task.FromResult(new JsonObject()));
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>())
            .Returns(_ => new ExpandoObject());
        _eventTypeMigrations.MigrateToAllGenerations(Arg.Any<EventStoreName>(), Arg.Any<EventType>(), Arg.Any<JsonObject>())
            .Returns(new Dictionary<EventTypeGeneration, ExpandoObject>());

        _currentValidation = _constraintValidation;
        _constraintValidationFactory.Create(Arg.Any<EventSequenceKey>()).Returns(_ => _currentValidation);
        _constraintValidation.Establish(
            Arg.Any<EventSourceId>(),
            Arg.Any<EventTypeId>(),
            Arg.Any<ExpandoObject>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<ConstraintBatchClaims?>())
            .Returns(callInfo => new ConstraintValidationContext(
                [_recordingValidator],
                callInfo.ArgAt<EventSourceId>(0),
                callInfo.ArgAt<EventTypeId>(1),
                callInfo.ArgAt<ExpandoObject>(2)));

        _eventSequenceStorage.Append(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<IEnumerable<IdentityId>>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<IDictionary<EventTypeGeneration, ExpandoObject>>(),
            Arg.Any<IDictionary<EventTypeGeneration, EventHash>>(),
            Arg.Any<Subject?>())
            .Returns(callInfo =>
            {
                var sequenceNumber = callInfo.ArgAt<EventSequenceNumber>(0);
                _appendedSequenceNumber = sequenceNumber;
                var appendedEvent = new AppendedEvent(
                    EventContext.From(
                        EventStore,
                        EventStoreNamespace,
                        _eventType,
                        EventSourceType.Default,
                        _eventSourceId,
                        EventStreamType.All,
                        EventStreamId.Default,
                        sequenceNumber,
                        CorrelationId.NotSet),
                    new ExpandoObject());
                return Task.FromResult<Result<AppendedEvent, DuplicateEventSequenceNumber>>(appendedEvent);
            });

        _namespaces = Substitute.For<INamespaces>();
        _appendedEventsQueues = Substitute.For<IAppendedEventsQueues>();

        _registeredConstraints = [];
        _constraintsGrain = Substitute.For<IConstraints>();
        _constraintsGrain.GetDefinitions().Returns(_ => _registeredConstraints.ToArray());
        _constraintsGrain.GetVersion().Returns(_ => ConstraintDefinitionComparison.ComputeVersion(_registeredConstraints));
        _jobsManager = Substitute.For<IJobsManager>();

        _silo.AddService(_storage);
        _silo.AddService(_constraintValidationFactory);
        _silo.AddService(_eventTypeMigrations);
        _silo.AddService(_complianceManager);
        _silo.AddService(_expandoObjectConverter);
        _silo.AddService(Substitute.For<IEventSerializer>());
        _silo.AddService(Substitute.For<IEventHashCalculator>());
        _silo.AddService(NullLogger<EventSequence>.Instance);
        _silo.AddKeyedService<IMeter<EventSequence>>(WellKnown.MeterName, Substitute.For<IMeter<EventSequence>>());
        _silo.AddKeyedService<IActivitySource<EventSequence>>(WellKnown.MeterName, new ActivitySource<EventSequence>());
        _silo.AddProbe(_ => _namespaces);
        _silo.AddProbe(_ => _appendedEventsQueues);
        _silo.AddProbe(_ => _constraintsGrain);
        _silo.AddProbe(_ => _jobsManager);

        _eventSequence = await _silo.CreateGrainAsync<EventSequence>(_eventSequenceKey.ToString());
    }
}
