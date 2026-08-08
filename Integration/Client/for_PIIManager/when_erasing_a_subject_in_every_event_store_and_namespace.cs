// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Integration.for_PIIManager.when_erasing_a_subject_in_every_event_store_and_namespace.context;

namespace Cratis.Chronicle.Integration.for_PIIManager;

[Collection(ChronicleCollection.Name)]
public class when_erasing_a_subject_in_every_event_store_and_namespace(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public const string SourceEventStoreName = "pii-fanout-source";
        public const string TargetEventStoreName = "pii-fanout-target";
        public const string SubscriptionId = "pii-fanout-source-to-target";

        /// <summary>
        /// Gets the event source the subject's event is appended to. Per run, so this spec and its sibling
        /// cannot see each other's keys through the kernel collection they share.
        /// </summary>
        public EventSourceId EventSourceId { get; } = $"request-{Guid.NewGuid():N}";
        public Subject Subject { get; } = $"person-{Guid.NewGuid():N}";
        public string SocialSecurityNumber { get; } = "111-22-3333";

        public bool SourceHasKeyBeforeErasure { get; private set; }
        public bool TargetHasKeyBeforeErasure { get; private set; }
        public IEnumerable<EventStoreName> EnumeratedEventStores { get; private set; } = [];
        public bool SourceHasKeyAfterErasure { get; private set; } = true;
        public bool TargetHasKeyAfterErasure { get; private set; } = true;
        public string PiiInSourceAfterErasure { get; private set; } = string.Empty;
        public string PiiInTargetAfterErasure { get; private set; } = string.Empty;

        public override IEnumerable<Type> EventTypes => [typeof(PersonRegistered)];

        async Task Because()
        {
            var keys = Services.GetRequiredService<IEncryptionKeyStorage>();
            var sourceEventStore = await ChronicleClient.GetEventStore(SourceEventStoreName);
            var targetEventStore = await ChronicleClient.GetEventStore(TargetEventStoreName);

            await Task.WhenAll(sourceEventStore.DiscoverAll(), targetEventStore.DiscoverAll());
            await Task.WhenAll(sourceEventStore.EventTypes.Register(), targetEventStore.EventTypes.Register());

            await targetEventStore.Subscriptions.Subscribe(
                new EventStoreSubscriptionId(SubscriptionId),
                SourceEventStoreName,
                builder => builder.WithEventType<PersonRegistered>());

            var systemTail = await targetEventStore.GetEventSequence(EventSequenceId.System).GetTailSequenceNumber();
            var subscriptionsReactor = await targetEventStore.Reactors.WaitForHandlerById(
                "$system.Cratis.Chronicle.Observation.EventStoreSubscriptions.EventStoreSubscriptionsReactor",
                TimeSpanFactory.DefaultTimeout());
            if (systemTail.IsActualValue)
            {
                await subscriptionsReactor.WaitTillReachesEventSequenceNumber(systemTail);
            }

            await sourceEventStore.GetEventSequence(EventSequenceId.Outbox).Append(
                EventSourceId,
                new PersonRegistered(Subject, "Jane Doe", SocialSecurityNumber));
            await WaitForInboxTail(targetEventStore, new EventSequenceId($"inbox-{SourceEventStoreName}"));

            SourceHasKeyBeforeErasure = await keys.HasFor(SourceEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);
            TargetHasKeyBeforeErasure = await keys.HasFor(TargetEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);

            // The complete erasure a consumer has to write today: enumerate every event store, then every
            // namespace within it, and delete in each. The enumeration is the part under specification and runs
            // unfiltered — the facts below assert that both of this spec's event stores come back from it. The
            // deletes are then confined to those two: every spec in this collection shares one kernel, so an
            // unfiltered fan-out would reach into event stores other specs created and delete inside them.
            EnumeratedEventStores = await ChronicleClient.GetEventStores();
            var ownEventStores = EnumeratedEventStores.Where(_ =>
                _ == new EventStoreName(SourceEventStoreName) || _ == new EventStoreName(TargetEventStoreName));

            foreach (var eventStoreName in ownEventStores)
            {
                var eventStore = await ChronicleClient.GetEventStore(eventStoreName);
                foreach (var @namespace in await eventStore.GetNamespaces())
                {
                    var scopedEventStore = await ChronicleClient.GetEventStore(eventStoreName, @namespace);
                    await scopedEventStore.PII.DeleteEncryptionKeyFor(Subject.Value);
                }
            }

            SourceHasKeyAfterErasure = await keys.HasFor(SourceEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);
            TargetHasKeyAfterErasure = await keys.HasFor(TargetEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);
            PiiInSourceAfterErasure = await ReadSocialSecurityNumber(sourceEventStore, EventSequenceId.Outbox);
            PiiInTargetAfterErasure = await ReadSocialSecurityNumber(targetEventStore, new EventSequenceId($"inbox-{SourceEventStoreName}"));
        }

        static async Task<string> ReadSocialSecurityNumber(IEventStore eventStore, EventSequenceId sequenceId)
        {
            var events = await eventStore.GetEventSequence(sequenceId).GetFromSequenceNumber(EventSequenceNumber.First);
            return ((PersonRegistered)events.First(_ => _.Context.SequenceNumber == EventSequenceNumber.First).Content).SocialSecurityNumber;
        }

        static async Task WaitForInboxTail(IEventStore targetEventStore, EventSequenceId inboxSequenceId)
        {
            var inbox = targetEventStore.GetEventSequence(inboxSequenceId);
            var deadline = DateTime.UtcNow.Add(TimeSpanFactory.DefaultTimeout());

            while (DateTime.UtcNow < deadline)
            {
                var tail = await inbox.GetTailSequenceNumber();
                if (tail.IsActualValue && tail >= EventSequenceNumber.First)
                {
                    return;
                }

                await Task.Delay(100);
            }

            throw new TimeoutException($"Inbox '{inboxSequenceId}' never received the forwarded event.");
        }
    }

    [Fact]
    void should_hold_the_key_in_the_source_event_store_before_erasure() =>
        Context.SourceHasKeyBeforeErasure.ShouldBeTrue();

    [Fact]
    void should_hold_the_key_in_the_target_event_store_before_erasure() =>
        Context.TargetHasKeyBeforeErasure.ShouldBeTrue();

    [Fact]
    void should_enumerate_the_source_event_store() =>
        Context.EnumeratedEventStores.ShouldContain(new EventStoreName(context.SourceEventStoreName));

    [Fact]
    void should_enumerate_the_target_event_store() =>
        Context.EnumeratedEventStores.ShouldContain(new EventStoreName(context.TargetEventStoreName));

    [Fact]
    void should_remove_the_key_from_the_source_event_store() =>
        Context.SourceHasKeyAfterErasure.ShouldBeFalse();

    [Fact]
    void should_remove_the_key_from_the_target_event_store() =>
        Context.TargetHasKeyAfterErasure.ShouldBeFalse();

    [Fact]
    void should_blank_the_pii_in_the_source_event_store() =>
        Context.PiiInSourceAfterErasure.ShouldEqual(string.Empty);

    [Fact]
    void should_blank_the_pii_in_the_target_event_store() =>
        Context.PiiInTargetAfterErasure.ShouldEqual(string.Empty);
}
