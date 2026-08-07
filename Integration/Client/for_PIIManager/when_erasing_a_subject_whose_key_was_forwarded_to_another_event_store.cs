// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Integration.for_PIIManager.when_erasing_a_subject_whose_key_was_forwarded_to_another_event_store.context;

namespace Cratis.Chronicle.Integration.for_PIIManager;

[Collection(ChronicleCollection.Name)]
public class when_erasing_a_subject_whose_key_was_forwarded_to_another_event_store(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public const string SourceEventStoreName = "pii-forwarding-source";
        public const string TargetEventStoreName = "pii-forwarding-target";
        public const string ForwardSubscriptionId = "pii-forwarding-source-to-target";
        public const string BackSubscriptionId = "pii-forwarding-target-to-source";

        public EventSourceId EventSourceId { get; } = "request-23";
        public Subject Subject { get; } = "person-23";
        public string SocialSecurityNumber { get; } = "111-22-3333";

        public bool SourceHasKeyAfterForwarding { get; private set; }
        public bool TargetHasKeyAfterForwarding { get; private set; }
        public bool KeyMaterialIsIdenticalAfterForwarding { get; private set; }

        public bool SourceHasKeyAfterErasure { get; private set; } = true;
        public bool TargetHasKeyAfterErasure { get; private set; }
        public string PiiInSourceAfterErasure { get; private set; } = string.Empty;
        public string PiiInTargetAfterErasure { get; private set; } = string.Empty;

        public bool SourceHasKeyAfterForwardingBack { get; private set; }
        public bool KeyMaterialIsRestoredAfterForwardingBack { get; private set; }
        public string PiiInSourceAfterForwardingBack { get; private set; } = string.Empty;

        public override IEnumerable<Type> EventTypes => [typeof(PersonRegistered)];

        async Task Because()
        {
            var keys = Services.GetRequiredService<IEncryptionKeyStorage>();
            var sourceEventStore = await ChronicleClient.GetEventStore(SourceEventStoreName);
            var targetEventStore = await ChronicleClient.GetEventStore(TargetEventStoreName);

            await Task.WhenAll(sourceEventStore.DiscoverAll(), targetEventStore.DiscoverAll());
            await Task.WhenAll(sourceEventStore.EventTypes.Register(), targetEventStore.EventTypes.Register());

            await Subscribe(targetEventStore, ForwardSubscriptionId, SourceEventStoreName);
            await Subscribe(sourceEventStore, BackSubscriptionId, TargetEventStoreName);

            // The subject's key is minted in the source store by the append, and the forwarding
            // subscriber copies it into the target store before it appends to the target's inbox.
            await sourceEventStore.GetEventSequence(EventSequenceId.Outbox).Append(
                EventSourceId,
                new PersonRegistered(Subject, "Jane Doe", SocialSecurityNumber));
            await WaitForInboxTail(TargetEventStoreName, SourceEventStoreName);

            var sourceKey = await keys.TryGetFor(SourceEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);
            var targetKey = await keys.TryGetFor(TargetEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);
            SourceHasKeyAfterForwarding = sourceKey is not null;
            TargetHasKeyAfterForwarding = targetKey is not null;
            KeyMaterialIsIdenticalAfterForwarding = sourceKey is not null && targetKey is not null &&
                sourceKey.Private.SequenceEqual(targetKey.Private) && sourceKey.Public.SequenceEqual(targetKey.Public);

            // The obvious erasure: the consumer holds the source event store and erases through it.
            await sourceEventStore.PII.DeleteEncryptionKeyFor(Subject.Value);

            SourceHasKeyAfterErasure = await keys.HasFor(SourceEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);
            TargetHasKeyAfterErasure = await keys.HasFor(TargetEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);
            PiiInSourceAfterErasure = await ReadSocialSecurityNumber(sourceEventStore, EventSequenceId.Outbox);
            PiiInTargetAfterErasure = await ReadSocialSecurityNumber(targetEventStore, InboxFrom(SourceEventStoreName));

            // Any later event for the same subject travelling the other way restores the erased key.
            await targetEventStore.GetEventSequence(EventSequenceId.Outbox).Append(
                EventSourceId,
                new PersonRegistered(Subject, "Jane Doe", SocialSecurityNumber));
            await WaitForInboxTail(SourceEventStoreName, TargetEventStoreName);

            var resurrectedKey = await keys.TryGetFor(SourceEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);
            SourceHasKeyAfterForwardingBack = resurrectedKey is not null;
            KeyMaterialIsRestoredAfterForwardingBack = resurrectedKey is not null && sourceKey is not null &&
                resurrectedKey.Private.SequenceEqual(sourceKey.Private) && resurrectedKey.Public.SequenceEqual(sourceKey.Public);
            PiiInSourceAfterForwardingBack = await ReadSocialSecurityNumber(sourceEventStore, EventSequenceId.Outbox);
        }

        static EventSequenceId InboxFrom(string sourceEventStoreName) => new($"inbox-{sourceEventStoreName}");

        static async Task<string> ReadSocialSecurityNumber(IEventStore eventStore, EventSequenceId sequenceId)
        {
            var events = await eventStore.GetEventSequence(sequenceId).GetFromSequenceNumber(EventSequenceNumber.First);
            return ((PersonRegistered)events.First(_ => _.Context.SequenceNumber == EventSequenceNumber.First).Content).SocialSecurityNumber;
        }

        async Task Subscribe(IEventStore targetEventStore, string subscriptionId, string sourceEventStoreName)
        {
            await targetEventStore.Subscriptions.Subscribe(
                new EventStoreSubscriptionId(subscriptionId),
                sourceEventStoreName,
                builder => builder.WithEventType<PersonRegistered>());

            var systemLog = targetEventStore.GetEventSequence(EventSequenceId.System);
            var systemTail = await systemLog.GetTailSequenceNumber();
            var subscriptionsReactor = await targetEventStore.Reactors.WaitForHandlerById(
                "$system.Cratis.Chronicle.Observation.EventStoreSubscriptions.EventStoreSubscriptionsReactor",
                TimeSpanFactory.DefaultTimeout());
            if (systemTail.IsActualValue)
            {
                await subscriptionsReactor.WaitTillReachesEventSequenceNumber(systemTail);
            }
        }

        async Task WaitForInboxTail(string targetEventStoreName, string sourceEventStoreName)
        {
            var targetEventStore = await ChronicleClient.GetEventStore(targetEventStoreName);
            var inbox = targetEventStore.GetEventSequence(InboxFrom(sourceEventStoreName));
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

            throw new TimeoutException($"Inbox '{InboxFrom(sourceEventStoreName)}' in '{targetEventStoreName}' never received the forwarded event.");
        }
    }

    [Fact]
    void should_keep_the_key_in_the_source_event_store_after_forwarding() =>
        Context.SourceHasKeyAfterForwarding.ShouldBeTrue();

    [Fact]
    void should_copy_the_key_into_the_target_event_store_when_forwarding() =>
        Context.TargetHasKeyAfterForwarding.ShouldBeTrue();

    [Fact]
    void should_copy_the_very_same_key_material() =>
        Context.KeyMaterialIsIdenticalAfterForwarding.ShouldBeTrue();

    [Fact]
    void should_remove_the_key_from_the_erased_event_store() =>
        Context.SourceHasKeyAfterErasure.ShouldBeFalse();

    [Fact]
    void should_leave_the_copy_in_the_other_event_store() =>
        Context.TargetHasKeyAfterErasure.ShouldBeTrue();

    [Fact]
    void should_blank_the_pii_in_the_erased_event_store() =>
        Context.PiiInSourceAfterErasure.ShouldEqual(string.Empty);

    [Fact]
    void should_still_expose_the_pii_in_the_other_event_store() =>
        Context.PiiInTargetAfterErasure.ShouldEqual(Context.SocialSecurityNumber);

    [Fact]
    void should_restore_the_key_when_an_event_is_forwarded_back() =>
        Context.SourceHasKeyAfterForwardingBack.ShouldBeTrue();

    [Fact]
    void should_restore_the_original_key_material() =>
        Context.KeyMaterialIsRestoredAfterForwardingBack.ShouldBeTrue();

    [Fact]
    void should_make_the_erased_pii_readable_again() =>
        Context.PiiInSourceAfterForwardingBack.ShouldEqual(Context.SocialSecurityNumber);
}
