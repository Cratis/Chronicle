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

public class and_multiple_events_share_subject : Specification
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
        _encryptionKeyStorage.HasFor("Lobby", EventStoreNamespaceName.Default, Arg.Any<EncryptionKeyIdentifier>()).Returns(false);
        _encryptionKeyStorage.HasFor("Admin", EventStoreNamespaceName.Default, Arg.Any<EncryptionKeyIdentifier>()).Returns(true);
        _encryptionKeyStorage.GetFor("Admin", EventStoreNamespaceName.Default, Arg.Any<EncryptionKeyIdentifier>()).Returns(new EncryptionKey([], []));
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
        dynamic firstContent = new ExpandoObject();
        firstContent.message = "first";
        dynamic secondContent = new ExpandoObject();
        secondContent.message = "second";

        var firstContext = EventContext.From(
            new EventStoreName("Admin"),
            EventStoreNamespaceName.Default,
            new EventType("event-type-id-1", EventTypeGeneration.First),
            EventSourceType.Default,
            new EventSourceId("source-1"),
            EventStreamType.All,
            EventStreamId.Default,
            42UL,
            CorrelationId.New(),
            subject: _explicitSubject);

        var secondContext = EventContext.From(
            new EventStoreName("Admin"),
            EventStoreNamespaceName.Default,
            new EventType("event-type-id-2", EventTypeGeneration.First),
            EventSourceType.Default,
            new EventSourceId("source-1"),
            EventStreamType.All,
            EventStreamId.Default,
            43UL,
            CorrelationId.New(),
            subject: _explicitSubject);

        await _subscriber.OnNext(
            new Key("partition", ArrayIndexers.NoIndexers),
            [new AppendedEvent(firstContext, firstContent), new AppendedEvent(secondContext, secondContent)],
            new ObserverSubscriberContext("Lobby"));
    }

    [Fact]
    Task should_only_copy_encryption_key_once() =>
        _encryptionKeyStorage.Received(1).SaveFor(
            "Lobby",
            EventStoreNamespaceName.Default,
            Arg.Is<EncryptionKeyIdentifier>(identifier => identifier.Value == _explicitSubject.Value),
            Arg.Any<EncryptionKey>());
}
