// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.many_with_duplicate_unique_value_among_distinct_events.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class many_with_duplicate_unique_value_among_distinct_events(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueUserConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(UserOnboardingStarted), typeof(UserRemoved)];

        public UserOnboardingStarted Distinct { get; private set; }
        public UserOnboardingStarted Duplicate { get; private set; }

        public IAppendResult Result { get; private set; }

        public void Establish()
        {
            Distinct = new UserOnboardingStarted(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            Duplicate = new UserOnboardingStarted(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        }

        public async Task Because()
        {
            // The duplicate appears as the second and third event for different sources, never as the first —
            // so the violation can only be caught by reconciling against the other events in the same batch.
            Result = await EventStore.EventLog.AppendMany(
            [
                new EventForEventSourceId(Guid.NewGuid().ToString(), Distinct),
                new EventForEventSourceId(Guid.NewGuid().ToString(), Duplicate),
                new EventForEventSourceId(Guid.NewGuid().ToString(), Duplicate)
            ]);
        }
    }

    [Fact] void should_not_be_successful() => Context.Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_constraint_violations() => Context.Result.HasConstraintViolations.ShouldBeTrue();
    [Fact] async Task should_not_commit_any_events() => (await Context.EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count.ShouldEqual(0);
}
