// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Integration.Base;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_EventSequence.when_appending.many_with_first_event_violating_unique_constraint.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_EventSequence.when_appending;

[Collection(GlobalCollection.Name)]
public class many_with_first_event_violating_unique_constraint(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : IntegrationSpecificationContext(globalFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueUserConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(UserOnboardingStarted), typeof(UserRemoved)];

        public UserOnboardingStarted Event { get; private set; }

        public AppendManyResult Result { get; private set; }

        public async Task Establish()
        {
            Event = new UserOnboardingStarted(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            await EventStore.EventLog.Append(Guid.NewGuid().ToString(), Event);
        }

        public async Task Because()
        {
            Result = await EventStore.EventLog.AppendMany(Guid.NewGuid().ToString(), [Event, new UserRemoved()]);
        }
    }

    [Fact] void should_not_succeed_on_second_attempt() => Context.Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_commit_any_of_the_two_events() => Context.EventLogSequenceGrain.GetTailSequenceNumber().Result.Value.ShouldEqual(EventSequenceNumber.First.Value);
}

