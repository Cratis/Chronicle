// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Captures.Engine;
using Cratis.Chronicle.Captures.Engine.DeclarationLanguage;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Captures;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriber.given;

public class a_capture_events_subscriber : Specification
{
    protected const string Declaration = """
        capture ShipmentTracking
          source events
            sequence inbox-fulfillment
            from ShipmentDispatched
            from ShipmentDelivered
          key $eventSourceId
          append OrderShipped
            when status from "pending" to "dispatched"
              orderId = $.orderId
              correlation = $context.correlationId
        """;

    protected TestKitSilo _silo;
    protected IStorage _storage;
    protected ICapturesStorage _captures;
    protected IEventSequenceStorage _log;
    protected IEventSequence _eventSequence;
    protected EventStoreName _eventStore;
    protected EventStoreNamespaceName _namespace;
    protected Capture _capture;
    protected Dictionary<CaptureId, CaptureObservation> _observations;
    protected List<EventToAppend> _appended;
    protected CorrelationId _appendCorrelation;
    protected IEnumerable<Causation> _appendCausation;
    protected int _appendCalls;
    protected AppendManyResult _appendResult;
    protected EventSequenceId _sequence = "inbox-fulfillment";
    protected ulong _alreadyTagged;

    void Establish()
    {
        _eventStore = "some-store";
        _namespace = "tenant-a";
        _capture = new Capture(CaptureId.New(), "ShipmentTracking", Declaration, CaptureStatus.Started);
        _observations = [];
        _appended = [];
        _appendCausation = [];
        _appendResult = AppendManyResult.Success(CorrelationId.New(), [EventSequenceNumber.First]);

        _storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _captures = Substitute.For<ICapturesStorage>();
        var eventTypes = Substitute.For<IEventTypesStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _log = Substitute.For<IEventSequenceStorage>();
        _eventSequence = Substitute.For<IEventSequence>();

        _storage.GetEventStore(_eventStore).Returns(eventStoreStorage);
        eventStoreStorage.Captures.Returns(_captures);
        eventStoreStorage.EventTypes.Returns(eventTypes);
        eventStoreStorage.GetNamespace(_namespace).Returns(namespaceStorage);
        namespaceStorage.GetEventSequence(EventSequenceId.Log).Returns(_log);
        _log.GetCount(Arg.Any<EventSequenceNumber?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>())
            .Returns(_ => Task.FromResult(new EventCount(_alreadyTagged)));

        _captures.Has(_capture.Id).Returns(true);
        _captures.Get(_capture.Id).Returns(_capture);
        _captures.GetObservation(Arg.Any<CaptureId>()).Returns(call =>
            _observations.TryGetValue(call.Arg<CaptureId>(), out var observation) ? observation : CaptureObservation.Empty(call.Arg<CaptureId>()));
        _captures.SaveObservation(Arg.Any<CaptureObservation>()).Returns(call =>
        {
            var observation = call.Arg<CaptureObservation>();
            _observations[observation.Id] = observation;
            return Task.CompletedTask;
        });

        eventTypes.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(true);
        eventTypes.GetFor(new EventTypeId("OrderShipped"), Arg.Any<EventTypeGeneration?>())
            .Returns(new EventTypeSchema(new EventType("OrderShipped", EventTypeGeneration.First), EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema()));

        _eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventToAppend>>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<ConcurrencyScopes>()).Returns(call =>
        {
            _appendCalls++;
            _appended.AddRange(call.Arg<IEnumerable<EventToAppend>>());
            _appendCorrelation = call.Arg<CorrelationId>();
            _appendCausation = call.Arg<IEnumerable<Causation>>();
            return _appendResult;
        });

        _silo = new TestKitSilo();
        _silo.AddService(_storage);
        _silo.AddService<ILanguageService>(new LanguageService());
        _silo.AddService<ICaptureEventTranslator>(new CaptureEventTranslator(new CaptureChangeDetector(), new WhenClauseEvaluator(), new CaptureContentMapper()));
        _silo.AddService(new JsonSerializerOptions());
        _silo.AddService(Substitute.For<ILogger<CaptureEventsSubscriber>>());
        _silo.AddProbe<IEventSequence>(_ => _eventSequence);
    }

    /// <summary>
    /// Gets or sets the sequence the subscriber is keyed on. The test silo only ever creates one grain, so the
    /// subscriber is created when first observing, which lets a spec choose the sequence before it does.
    /// </summary>
    protected EventSequenceId ObservedSequence { get; set; } = "inbox-fulfillment";

    protected CaptureEventsSubscriber Subscriber { get; private set; }

    async Task<CaptureEventsSubscriber> CreateSubscriber(EventSequenceId sequence)
    {
        var key = new ObserverSubscriberKey(
            CaptureObservers.For(_capture.Id),
            _eventStore,
            _namespace,
            sequence,
            ObserverSubscriberKey.AllPartitions,
            "127.0.0.1:11111@1");
        return await _silo.CreateGrainAsync<CaptureEventsSubscriber>(key.ToString());
    }

    protected async Task<ObserverSubscriberResult> Observe(params AppendedEvent[] events)
    {
        Subscriber ??= await CreateSubscriber(ObservedSequence);
        return await Subscriber.OnNext((Key)"some-key", events, new ObserverSubscriberContext(null));
    }

    protected AppendedEvent Incoming(
        ulong sequenceNumber,
        string eventType,
        string status,
        string eventSourceId = "shipment-1",
        CorrelationId? correlationId = null,
        DateTimeOffset? occurred = null,
        Subject? subject = null)
    {
        dynamic content = new ExpandoObject();
        content.orderId = "order-1";
        content.status = status;

        var context = EventContext.From(
            _eventStore,
            _namespace,
            new EventType(eventType, EventTypeGeneration.First),
            EventSourceType.Default,
            new EventSourceId(eventSourceId),
            EventStreamType.All,
            EventStreamId.Default,
            new EventSequenceNumber(sequenceNumber),
            correlationId ?? CorrelationId.New(),
            occurred: occurred,
            subject: subject);

        return new AppendedEvent(context, content);
    }
}
