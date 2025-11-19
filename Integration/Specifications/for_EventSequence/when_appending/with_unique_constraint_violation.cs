// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_appending.with_unique_constraint_violation.context;

namespace Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class with_unique_constraint_violation(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueUserConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(UserOnboardingStarted), typeof(UserRemoved)];

        public UserOnboardingStarted Event { get; private set; }

        public AppendResult FirstResult { get; private set; }
        public AppendResult SecondResult { get; private set; }

        public void Establish()
        {
            Event = new UserOnboardingStarted(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        }

        public async Task Because()
        {
            FirstResult = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), Event);
            SecondResult = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), Event);
        }
    }

    [Fact] void should_succeed_on_first_attempt() => Context.FirstResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_succeed_on_second_attempt() => Context.SecondResult.IsSuccess.ShouldBeFalse();
}
