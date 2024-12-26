// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Integration.Base;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_EventSequence.when_appending.with_unique_event_violation.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_EventSequence.when_appending;

[Collection(GlobalCollection.Name)]
public class with_unique_event_violation(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : IntegrationSpecificationContext(globalFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueEventConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(UserOnboardingStarted)];

        public AppendResult FirstResult { get; private set; }
        public AppendResult SecondResult { get; private set; }
        UserOnboardingStarted _event;

        public void Establish() => _event = new UserOnboardingStarted(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        public async Task Because()
        {
            var eventSourceId = Guid.NewGuid().ToString();
            FirstResult = await EventStore.EventLog.Append(eventSourceId, _event);
            SecondResult = await EventStore.EventLog.Append(eventSourceId, _event);
        }
    }

    [Fact] void should_succeed_on_first_attempt() => Context.FirstResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_succeed_on_second_attempt() => Context.SecondResult.IsSuccess.ShouldBeFalse();
}
