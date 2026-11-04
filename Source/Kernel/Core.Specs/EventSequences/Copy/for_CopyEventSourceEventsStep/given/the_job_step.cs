// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.Copy.for_CopyEventSourceEventsStep.given;

public class the_job_step : Specification
{
    protected const string StoreName = "test-event-store";
    protected const string NamespaceName = "default";

    protected CopyEventSourceEventsStep _jobStep;
    protected IPersistentState<CopyEventSourceEventsStepState> _stateStorage;
    protected TestKitSilo _silo = new();
    protected IEventSequence _sequence;
    protected IEventSequenceStorage _sequenceStorage;
    protected EventSourceId _sourceId;
    protected EventSourceId _targetId;
    protected EventTypeId _eventTypeId;
    protected CopyEventSourceEventsRequest _request;
    protected List<EventToAppend> _appended = [];
    protected List<IdempotencyKey?> _keys = [];
    protected Catch<JobStepResult> _result;
    protected JobStepKey _jobStepKey;
    protected IEnumerable<EventType>? _requestedEventTypes;
    protected EventSourceId? _requestedSource;

    protected virtual IEnumerable<EventTypeId> RequestedEventTypes => [];

    async Task Establish()
    {
        _sourceId = "source";
        _targetId = "target";
        _eventTypeId = new EventTypeId("copied-event");
        _request = new CopyEventSourceEventsRequest(EventSequenceId.Log, _sourceId, _targetId, RequestedEventTypes.ToArray());

        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string"}}}""");
        var eventTypes = Substitute.For<IEventTypesStorage>();
        eventTypes.GetFor(_eventTypeId, 1).Returns(new EventTypeSchema(new EventType(_eventTypeId, 1), EventTypeOwner.Server, EventTypeSource.Code, schema));

        _sequenceStorage = Substitute.For<IEventSequenceStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(_sequenceStorage);
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.EventTypes.Returns(eventTypes);
        eventStoreStorage.GetNamespace(NamespaceName).Returns(namespaceStorage);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(StoreName).Returns(eventStoreStorage);

        _sequenceStorage.GetFromSequenceNumber(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceId?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<IEnumerable<EventType>?>(),
            Arg.Any<IEnumerable<Tag>?>(),
            Arg.Any<CancellationToken>()).Returns(call =>
            {
                _requestedSource = call.ArgAt<EventSourceId?>(1);
                _requestedEventTypes = call.ArgAt<IEnumerable<EventType>?>(5);
                return Task.FromResult(CursorOver(SourceEvents()));
            });

        _sequence = Substitute.For<IEventSequence>();
        _sequence.AppendManyWithIdempotencyKeys(
            Arg.Any<IEnumerable<EventToAppend>>(),
            Arg.Any<IReadOnlyList<IdempotencyKey?>>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<ConcurrencyScopes>()).Returns(call =>
            {
                _appended.AddRange(call.ArgAt<IEnumerable<EventToAppend>>(0));
                _keys.AddRange(call.ArgAt<IReadOnlyList<IdempotencyKey?>>(1));
                return Task.FromResult(new IdempotentAppendManyResult(AppendManyResult.Success(CorrelationId.New(), [])));
            });
        _silo.AddProbe<IEventSequence>(_ => _sequence);

        _silo.AddService(storage);
        _silo.AddService(Substitute.For<IJsonSchemaMetadataManager>());
        _silo.AddService<IExpandoObjectConverter>(new ExpandoObjectConverter(new TypeFormats()));
        _silo.AddService(new JsonSerializerOptions());
        _silo.AddService(Substitute.For<IJobStepThrottle>());
        var logger = _silo.AddService(NullLogger<CopyEventSourceEventsStep>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

        _jobStepKey = new JobStepKey(JobId.New(), StoreName, NamespaceName);
        _stateStorage = _silo.AddPersistentStateStorage<CopyEventSourceEventsStepState>(
            nameof(CopyEventSourceEventsStepState),
            Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps);

        _jobStep = await _silo.CreateGrainAsync<CopyEventSourceEventsStep>(JobStepId.New(), _jobStepKey);
    }

    protected virtual IEnumerable<AppendedEvent> SourceEvents() =>
        [SourceEvent(1, "first"), SourceEvent(2, "second")];

    protected AppendedEvent SourceEvent(ulong sequenceNumber, string name)
    {
        dynamic content = new ExpandoObject();
        content.name = name;
        var context = EventContext.EmptyWithEventSourceId(_sourceId) with
        {
            EventType = new EventType(_eventTypeId, 1),
            SequenceNumber = sequenceNumber,
            Tags = [new Tag("a-tag")]
        };
        return new AppendedEvent(context, (ExpandoObject)content);
    }

    protected async Task Perform()
    {
        await _jobStep.Prepare(_request);
        _result = await (Task<Catch<JobStepResult>>)typeof(CopyEventSourceEventsStep)
            .GetMethod("PerformStep", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_jobStep, [_stateStorage.State, CancellationToken.None])!;
    }

    static IEventCursor CursorOver(IEnumerable<AppendedEvent> events)
    {
        var cursor = Substitute.For<IEventCursor>();
        var moved = false;
        cursor.MoveNext().Returns(_ =>
        {
            var has = !moved;
            moved = true;
            return Task.FromResult(has);
        });
        cursor.Current.Returns(_ => events);
        return cursor;
    }
}
