// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_with_scoped_constraint.and_same_value_under_different_event_source_types.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_with_scoped_constraint;

[Collection(ChronicleCollection.Name)]
public class and_same_value_under_different_event_source_types(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : Specification(fixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(ScopedUniqueUserConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(ScopedUserRegistered)];

        public IAppendResult FirstResult { get; private set; }
        public IAppendResult SecondResult { get; private set; }

        public async Task Because()
        {
            var @event = new ScopedUserRegistered("john_doe");

            FirstResult = await EventStore.EventLog.Append(
                Guid.NewGuid().ToString(),
                @event,
                eventSourceType: "TypeA");

            SecondResult = await EventStore.EventLog.Append(
                Guid.NewGuid().ToString(),
                @event,
                eventSourceType: "TypeB");
        }
    }

    [Fact] void should_succeed_on_first_attempt() => Context.FirstResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_on_second_attempt_with_different_source_type() => Context.SecondResult.IsSuccess.ShouldBeTrue();
}
