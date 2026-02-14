// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_appending.with_constraint_value_added_and_then_removed.context;

namespace Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class with_constraint_value_added_and_then_removed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueUserConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(UserOnboardingStarted), typeof(UserRemoved)];

        UserOnboardingStarted _event;
        UserRemoved _removedEvent;

        public AppendResult FirstResult { get; private set; }
        public AppendResult RemovedResult { get; private set; }
        public AppendResult SecondResult { get; private set; }

        public void Establish()
        {
            _event = new UserOnboardingStarted(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            _removedEvent = new UserRemoved();
        }

        public async Task Because()
        {
            var firstEventSourceId = Guid.NewGuid().ToString();
            var secondEventSourceId = Guid.NewGuid().ToString();
            FirstResult = await EventStore.EventLog.Append(firstEventSourceId, _event);
            RemovedResult = await EventStore.EventLog.Append(firstEventSourceId, _removedEvent);
            SecondResult = await EventStore.EventLog.Append(secondEventSourceId, _event);
        }
    }

    [Fact] void should_succeed_on_first_attempt() => Context.FirstResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_on_remove_attempt() => Context.RemovedResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_on_second_attempt() => Context.SecondResult.IsSuccess.ShouldBeTrue();
}
