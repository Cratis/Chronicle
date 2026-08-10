// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Seeding;
using context = Cratis.Chronicle.Integration.for_EventSeeding.when_a_seeded_batch_is_rejected.context;

namespace Cratis.Chronicle.Integration.for_EventSeeding;

/// <summary>
/// The whole chain, against a real kernel and a real store. A seed set with one entry that violates a
/// constraint is rejected as a whole - appending many validates before it writes - so nothing lands. What
/// used to happen next is that every entry in it was recorded as seeded anyway, in the namespace grain and
/// in the global one, and the seed set was skipped forever after: correcting the seed data and running
/// again changed nothing, because the kernel already believed the events were there. The second run here is
/// that correction. It carries the innocent entry and the good half of the offending pair, and both must
/// land - which they only can if the rejected run recorded nothing, at either level.
/// </summary>
/// <param name="context">The context the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class when_a_seeded_batch_is_rejected(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public string TheOffice;
        public string TheOtherOffice;
        public Exception FirstFailure;
        public int EventsAfterTheRejectedRun;
        public IEnumerable<string> EventTypesAfterTheCorrectedRun;

        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueBadgeConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(BadgeIssued), typeof(OfficeOpened)];

        void Establish()
        {
            TheOffice = Guid.NewGuid().ToString();
            TheOtherOffice = Guid.NewGuid().ToString();
        }

        async Task Because()
        {
            // The same badge number claimed by two different event sources is a violation, and it takes the
            // whole batch down with it. OfficeOpened is the innocent entry: it shares nothing with the
            // offending pair but the batch it happens to sit in.
            EventStore.Seeding.ForEventSource(TheOffice, [new OfficeOpened("Bergen"), new BadgeIssued("A-1", "First holder")]);
            EventStore.Seeding.ForEventSource(TheOtherOffice, [new BadgeIssued("A-1", "Second holder")]);
            FirstFailure = await Catch.Exception(() => EventStore.Seeding.Register());

            EventsAfterTheRejectedRun = (await AllSeededEvents()).Count;

            // A failed register deliberately retains the offered entries so an unchanged process can retry
            // transient failures. Corrected seed definitions arrive with a new process and therefore a fresh
            // seeding buffer. It still uses the fixture's real event-store connection, registered event types and
            // serializer configuration, so the corrected call crosses the same client/kernel boundary without
            // reconnecting the shared fixture and replaying its retained failed buffer.
            var correctedSeeding = EventStore.CreateEventSeeding();
            correctedSeeding.ForEventSource(TheOffice, [new OfficeOpened("Bergen"), new BadgeIssued("A-1", "First holder")]);
            await correctedSeeding.Register();

            EventTypesAfterTheCorrectedRun =
            [
                .. (await AllSeededEvents())
                    .OrderBy(_ => _.Context.SequenceNumber.Value)
                    .Select(_ => _.Content.GetType().Name)
            ];
        }

        async Task<IReadOnlyList<AppendedEvent>> AllSeededEvents() =>
            await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
    }

    [Fact] void should_fail_the_rejected_run() => Context.FirstFailure.ShouldNotBeNull();
    [Fact] void should_append_nothing_from_the_rejected_run() => Context.EventsAfterTheRejectedRun.ShouldEqual(0);
    [Fact] void should_append_the_corrected_seed_set() => Context.EventTypesAfterTheCorrectedRun.Count().ShouldEqual(2);
    [Fact] void should_append_the_innocent_entry_that_shared_the_rejected_batch() => Context.EventTypesAfterTheCorrectedRun.ShouldContain(nameof(OfficeOpened));
    [Fact] void should_append_the_entry_that_is_no_longer_in_violation() => Context.EventTypesAfterTheCorrectedRun.ShouldContain(nameof(BadgeIssued));
}
