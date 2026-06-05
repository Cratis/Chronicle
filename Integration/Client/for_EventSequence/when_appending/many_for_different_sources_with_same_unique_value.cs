// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.many_for_different_sources_with_same_unique_value.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class many_for_different_sources_with_same_unique_value(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueUserConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(UserOnboardingStarted), typeof(UserRemoved)];

        public UserOnboardingStarted Event { get; private set; }

        public IAppendResult Result { get; private set; }

        public void Establish() => Event = new UserOnboardingStarted(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        public async Task Because() =>
            Result = await EventStore.EventLog.AppendMany(
            [
                new EventForEventSourceId(Guid.NewGuid().ToString(), Event),
                new EventForEventSourceId(Guid.NewGuid().ToString(), Event)
            ]);
    }

    [Fact] void should_not_be_successful() => Context.Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_constraint_violations() => Context.Result.HasConstraintViolations.ShouldBeTrue();
    [Fact] async Task should_not_commit_any_of_the_two_events() => (await Context.EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count.ShouldEqual(0);
}
