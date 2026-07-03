// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_PIIManager.when_deleting_the_encryption_key_for_a_subject.context;

namespace Cratis.Chronicle.Integration.for_PIIManager;

[Collection(ChronicleCollection.Name)]
public class when_deleting_the_encryption_key_for_a_subject(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "request-42";
        public Subject Subject { get; } = "person-42";
        public PersonRegistered Event { get; private set; }
        public string SocialSecurityNumberBeforeErasure { get; private set; }
        public AppendedEvent ReadEventAfterErasure { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(PersonRegistered)];

        void Establish() => Event = new PersonRegistered(Subject, "Jane Doe", "111-22-3333");

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event);

            // Read back decrypted — proves the PII round-trips before erasure.
            SocialSecurityNumberBeforeErasure = ((PersonRegistered)(await ReadEvent()).Content).SocialSecurityNumber;

            // Trigger client-side right-to-erasure — the seam a downstream app uses via IPIIManager
            // (IEventStore.PII resolves the same DI-registered instance) — crypto-shredding the subject's key.
            await EventStore.PII.DeleteEncryptionKeyFor(Subject);

            // Read again — must not throw; the PII property must now be empty.
            ReadEventAfterErasure = await ReadEvent();
        }

        async Task<AppendedEvent> ReadEvent()
        {
            var events = await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
            return events.First(_ => _.Context.SequenceNumber == EventSequenceNumber.First);
        }
    }

    [Fact]
    void should_have_decrypted_the_pii_before_erasure() =>
        Context.SocialSecurityNumberBeforeErasure.ShouldEqual(Context.Event.SocialSecurityNumber);

    [Fact]
    void should_be_able_to_read_the_event_after_erasure() =>
        Context.ReadEventAfterErasure.ShouldNotBeNull();

    [Fact]
    void should_return_empty_pii_after_erasure() =>
        ((PersonRegistered)Context.ReadEventAfterErasure.Content).SocialSecurityNumber.ShouldEqual(string.Empty);

    [Fact]
    void should_retain_non_pii_content_after_erasure() =>
        ((PersonRegistered)Context.ReadEventAfterErasure.Content).Name.ShouldEqual(Context.Event.Name);
}
