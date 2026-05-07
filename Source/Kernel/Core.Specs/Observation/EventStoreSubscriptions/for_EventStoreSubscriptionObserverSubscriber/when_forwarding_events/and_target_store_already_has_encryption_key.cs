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
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class and_target_store_already_has_encryption_key : Specification
{
    static readonly Subject _explicitSubject = "person-123";

    TestKitSilo _silo = null!;
    EventStoreSubscriptionObserverSubscriber _subscriber = null!;
    IEventSequence _inboxSequence = null!;
    IEncryptionKeyStorage _encryptionKeyStorage = null!;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _silo.AddService(new JsonSerializerOptions());
        _silo.AddService(Substitute.For<ILogger<EventStoreSubscriptionObserverSubscriber>>());

        _encryptionKeyStorage = Substitute.For<IEncryptionKeyStorage>();
        _encryptionKeyStorage.HasFor("Lobby", EventStoreNamespaceName.Default, Arg.Any<EncryptionKeyIdentifier>()).Returns(true);
        _silo.AddService(_encryptionKeyStorage);

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
            Arg.Any<Subject?>()).Returns(AppendResult.Success(CorrelationId.New(), 1UL));
        _silo.AddProbe(_ => _inboxSequence);

        var subscriberKey = new ObserverSubscriberKey(
            new ObserverId("subscription"),
            new EventStoreName("Admin"),
            EventStoreNamespaceName.Default,
            EventSequenceId.Outbox,
            new EventSourceId("partition-1"),
            "127.0.0.1:11111@1");
        _subscriber = await _silo.CreateGrainAsync<EventStoreSubscriptionObserverSubscriber>(subscriberKey.ToString());
    }

    async Task Because()
    {
        dynamic content = new ExpandoObject();
        content.message = "hello";

        var context = EventContext.From(
            new EventStoreName("Admin"),
            EventStoreNamespaceName.Default,
            new EventType("event-type-id", EventTypeGeneration.First),
            EventSourceType.Default,
            new EventSourceId("source-1"),
            EventStreamType.All,
            EventStreamId.Default,
            42UL,
            CorrelationId.New(),
            subject: _explicitSubject);

        await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [new AppendedEvent(context, content)],
            new ObserverSubscriberContext("Lobby"));
    }

    [Fact]
    Task should_not_copy_encryption_key_to_target_event_store() =>
        _encryptionKeyStorage.DidNotReceive().SaveFor(
            Arg.Any<EventStoreName>(),
            Arg.Any<EventStoreNamespaceName>(),
            Arg.Any<EncryptionKeyIdentifier>(),
            Arg.Any<EncryptionKey>());
}
