// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.given;

public class a_subscriber_with_events : Specification
{
    protected IEncryptionKeyStorage _encryptionKeyStorage = null!;
    protected IStorage _storage = null!;
    protected IEventGenerationRelease _generationRelease = null!;
    protected EventStoreSubscriptionObserverSubscriber _subscriber = null!;
    protected IEventSequence _inboxSequence = null!;
    protected AppendedEvent[] _events = null!;
    protected Func<EventType, AppendResult> _appendResultFor = _ => AppendResult.Success(CorrelationId.New(), 1UL);

    async Task Establish()
    {
        var silo = new TestKitSilo();
        _storage = Substitute.For<IStorage>();
        _generationRelease = CreateGenerationRelease();
        silo.AddService(_storage);
        silo.AddService(_generationRelease);
        silo.AddService(new JsonSerializerOptions());
        silo.AddService(Substitute.For<ILogger<EventStoreSubscriptionObserverSubscriber>>());
        _encryptionKeyStorage = Substitute.For<IEncryptionKeyStorage>();
        _encryptionKeyStorage.HasFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>()).Returns(true);
        silo.AddService(_encryptionKeyStorage);

        _inboxSequence = Substitute.For<IEventSequence>();
        _inboxSequence.Append(
            Arg.Any<EventSourceType>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventType>(),
            Arg.Any<JsonObject>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScope>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Subject?>()).Returns(call => _appendResultFor(call.ArgAt<EventType>(4)));
        silo.AddProbe(_ => _inboxSequence);

        var subscriberKey = new ObserverSubscriberKey(
            new ObserverId("subscription->Lobby"),
            new EventStoreName("Admin"),
            EventStoreNamespaceName.Default,
            EventSequenceId.Outbox,
            new EventSourceId("partition-1"),
            "127.0.0.1:11111@1");
        _subscriber = await silo.CreateGrainAsync<EventStoreSubscriptionObserverSubscriber>(subscriberKey.ToString());
        _events = [CreateEvent(42UL, "event-type-1"), CreateEvent(43UL, "event-type-2"), CreateEvent(44UL, "event-type-3")];
    }

    protected virtual IEventGenerationRelease CreateGenerationRelease() => Substitute.For<IEventGenerationRelease>();

    protected Task<ObserverSubscriberResult> Forward() =>
        _subscriber.OnNext(new Key("partition", ArrayIndexers.NoIndexers), _events, new ObserverSubscriberContext("Lobby"));

    protected int AppendCalls() => _inboxSequence.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append));

    static AppendedEvent CreateEvent(EventSequenceNumber sequenceNumber, EventTypeId eventTypeId)
    {
        dynamic content = new ExpandoObject();
        content.message = "private-content-marker";
        var context = EventContext.From(
            new EventStoreName("Admin"),
            EventStoreNamespaceName.Default,
            new EventType(eventTypeId, EventTypeGeneration.First),
            EventSourceType.Default,
            new EventSourceId("source-1"),
            EventStreamType.All,
            EventStreamId.Default,
            sequenceNumber,
            CorrelationId.New());

        return new AppendedEvent(context, content);
    }
}
