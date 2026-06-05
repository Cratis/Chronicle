// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.many_for_different_sources_with_distinct_unique_values.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class many_for_different_sources_with_distinct_unique_values(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueUserConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(UserOnboardingStarted), typeof(UserRemoved)];

        public IAppendResult Result { get; private set; }

        public async Task Because() =>
            Result = await EventStore.EventLog.AppendMany(
            [
                new EventForEventSourceId(Guid.NewGuid().ToString(), new UserOnboardingStarted(Guid.NewGuid().ToString(), Guid.NewGuid().ToString())),
                new EventForEventSourceId(Guid.NewGuid().ToString(), new UserOnboardingStarted(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()))
            ]);
    }

    [Fact] void should_be_successful() => Context.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_have_constraint_violations() => Context.Result.HasConstraintViolations.ShouldBeFalse();
    [Fact] async Task should_commit_both_events() => (await Context.EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count.ShouldEqual(2);
}
