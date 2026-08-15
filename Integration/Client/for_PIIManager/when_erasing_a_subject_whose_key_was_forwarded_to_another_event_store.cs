// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Integration.for_PIIManager.when_erasing_a_subject_whose_key_was_forwarded_to_another_event_store.context;

namespace Cratis.Chronicle.Integration.for_PIIManager;

/// <summary>
/// Records what a cross-event-store subscription does to an erasure, end to end against a real silo and a real
/// MongoDB - and what the erasure fence does to it in turn.
/// </summary>
/// <remarks>
/// <para>
/// This spec used to pin the defect: forwarding copied the subject's key into the second event store, one erasure
/// reached only the store it was addressed at, and a later forwarded event copied the survivor back and made
/// already-shredded personal data readable again. Those three facts are inverted here, which is what they were
/// written to become.
/// </para>
/// <para>
/// It walks the whole lifecycle in one run: the copy still happens before the erasure, one erasure now clears both
/// event stores, appending the subject's personal data afterwards fails rather than quietly minting a key, and an
/// explicitly authorized new lifecycle gives them a fresh key that cannot read a word of what came before.
/// </para>
/// </remarks>
/// <param name="context">The context the facts assert against.</param>
[Collection(ChronicleCollection.Name)]
public class when_erasing_a_subject_whose_key_was_forwarded_to_another_event_store(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public const string SourceEventStoreName = "pii-forwarding-source";
        public const string TargetEventStoreName = "pii-forwarding-target";
        public const string ForwardSubscriptionId = "pii-forwarding-source-to-target";

        /// <summary>
        /// Gets the event source the subject's event is appended to. Per run, so this spec and its sibling
        /// cannot see each other's keys through the kernel collection they share.
        /// </summary>
        public EventSourceId EventSourceId { get; } = $"request-{Guid.NewGuid():N}";
        public Subject Subject { get; } = $"person-{Guid.NewGuid():N}";
        public string SocialSecurityNumber { get; } = "111-22-3333";
        public string NewSocialSecurityNumber { get; } = "444-55-6666";

        public bool SourceHasKeyAfterForwarding { get; private set; }
        public bool TargetHasKeyAfterForwarding { get; private set; }
        public bool KeyMaterialIsIdenticalAfterForwarding { get; private set; }

        public bool SourceHasKeyAfterErasure { get; private set; } = true;
        public bool TargetHasKeyAfterErasure { get; private set; } = true;
        public string PiiInSourceAfterErasure { get; private set; } = string.Empty;
        public string PiiInTargetAfterErasure { get; private set; } = string.Empty;
        public bool SourceIsFencedAfterErasure { get; private set; }
        public bool TargetIsFencedAfterErasure { get; private set; }

        public bool AppendSucceededWhileErased { get; private set; } = true;
        public bool SourceHasKeyAfterAppendingWhileErased { get; private set; } = true;

        public bool AppendSucceededAfterAuthorizing { get; private set; }
        public bool KeyMaterialIsFreshAfterAuthorizing { get; private set; }
        public string PiiOfTheErasedEventAfterAuthorizing { get; private set; } = string.Empty;
        public string PiiOfTheNewEventAfterAuthorizing { get; private set; } = string.Empty;

        public override IEnumerable<Type> EventTypes => [typeof(PersonRegistered)];

        async Task Because()
        {
            var keys = Services.GetRequiredService<IEncryptionKeyStorage>();
            var sourceEventStore = await ChronicleClient.GetEventStore(SourceEventStoreName);
            var targetEventStore = await ChronicleClient.GetEventStore(TargetEventStoreName);

            await Task.WhenAll(sourceEventStore.DiscoverAll(), targetEventStore.DiscoverAll());
            await Task.WhenAll(sourceEventStore.EventTypes.Register(), targetEventStore.EventTypes.Register());

            await Subscribe(targetEventStore, ForwardSubscriptionId, SourceEventStoreName);

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

            // One call, through whichever event store the consumer happens to hold.
            await sourceEventStore.PII.DeleteEncryptionKeyFor(Subject.Value);

            SourceHasKeyAfterErasure = await keys.HasFor(SourceEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);
            TargetHasKeyAfterErasure = await keys.HasFor(TargetEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);
            SourceIsFencedAfterErasure = await keys.GetErasureFor(SourceEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value) is not null;
            TargetIsFencedAfterErasure = await keys.GetErasureFor(TargetEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value) is not null;
            PiiInSourceAfterErasure = await ReadSocialSecurityNumber(sourceEventStore, EventSequenceId.Outbox, EventSequenceNumber.First);
            PiiInTargetAfterErasure = await ReadSocialSecurityNumber(targetEventStore, InboxFrom(SourceEventStoreName), EventSequenceNumber.First);

            // Any later event for the same subject used to bring the key back. It now fails instead - loudly,
            // rather than quietly restarting protection for a person who asked to be forgotten.
            AppendSucceededWhileErased = await TryAppendPersonalData(sourceEventStore, SocialSecurityNumber);
            SourceHasKeyAfterAppendingWhileErased = await keys.HasFor(SourceEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);

            // The same person, with a lawful basis to be protected again. This creates no key; the next append does.
            await sourceEventStore.PII.AllowNewEncryptionKeyFor(Subject.Value);
            AppendSucceededAfterAuthorizing = await TryAppendPersonalData(sourceEventStore, NewSocialSecurityNumber);

            var freshKey = await keys.TryGetFor(SourceEventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);
            KeyMaterialIsFreshAfterAuthorizing = freshKey is not null && sourceKey is not null &&
                !freshKey.Private.SequenceEqual(sourceKey.Private) && !freshKey.Public.SequenceEqual(sourceKey.Public);

            PiiOfTheErasedEventAfterAuthorizing = await ReadSocialSecurityNumber(sourceEventStore, EventSequenceId.Outbox, EventSequenceNumber.First);
            PiiOfTheNewEventAfterAuthorizing = await ReadSocialSecurityNumber(sourceEventStore, EventSequenceId.Outbox, new EventSequenceNumber(EventSequenceNumber.First.Value + 1UL));
        }

        static EventSequenceId InboxFrom(string sourceEventStoreName) => new($"inbox-{sourceEventStoreName}");

        static async Task<string> ReadSocialSecurityNumber(IEventStore eventStore, EventSequenceId sequenceId, EventSequenceNumber sequenceNumber)
        {
            var events = await eventStore.GetEventSequence(sequenceId).GetFromSequenceNumber(EventSequenceNumber.First);
            var appended = events.FirstOrDefault(_ => _.Context.SequenceNumber == sequenceNumber);
            return appended is null ? string.Empty : ((PersonRegistered)appended.Content).SocialSecurityNumber;
        }

        async Task<bool> TryAppendPersonalData(IEventStore eventStore, string socialSecurityNumber)
        {
            // Whether the refusal arrives as a failed result or as an exception is a transport detail; what the
            // spec is about is that it is not silently accepted.
            var succeeded = false;
            var error = await Catch.Exception(async () =>
            {
                var result = await eventStore.GetEventSequence(EventSequenceId.Outbox).Append(
                    EventSourceId,
                    new PersonRegistered(Subject, "Jane Doe", socialSecurityNumber));
                succeeded = result.IsSuccess;
            });

            return error is null && succeeded;
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
    void should_remove_the_copy_from_the_other_event_store() =>
        Context.TargetHasKeyAfterErasure.ShouldBeFalse();

    [Fact]
    void should_fence_the_erased_event_store() =>
        Context.SourceIsFencedAfterErasure.ShouldBeTrue();

    [Fact]
    void should_fence_the_event_store_the_key_was_copied_into() =>
        Context.TargetIsFencedAfterErasure.ShouldBeTrue();

    [Fact]
    void should_blank_the_pii_in_the_erased_event_store() =>
        Context.PiiInSourceAfterErasure.ShouldEqual(string.Empty);

    [Fact]
    void should_blank_the_pii_in_the_other_event_store() =>
        Context.PiiInTargetAfterErasure.ShouldEqual(string.Empty);

    [Fact]
    void should_refuse_to_store_the_subjects_personal_data_while_they_are_erased() =>
        Context.AppendSucceededWhileErased.ShouldBeFalse();

    [Fact]
    void should_not_mint_a_key_for_the_erased_subject() =>
        Context.SourceHasKeyAfterAppendingWhileErased.ShouldBeFalse();

    [Fact]
    void should_protect_the_subject_again_once_a_new_key_is_authorized() =>
        Context.AppendSucceededAfterAuthorizing.ShouldBeTrue();

    [Fact]
    void should_give_the_new_lifecycle_key_material_of_its_own() =>
        Context.KeyMaterialIsFreshAfterAuthorizing.ShouldBeTrue();

    [Fact]
    void should_keep_the_erased_pii_unreadable_under_the_new_key() =>
        Context.PiiOfTheErasedEventAfterAuthorizing.ShouldEqual(string.Empty);

    [Fact]
    void should_read_back_the_pii_written_after_the_new_lifecycle_began() =>
        Context.PiiOfTheNewEventAfterAuthorizing.ShouldEqual(Context.NewSocialSecurityNumber);
}
