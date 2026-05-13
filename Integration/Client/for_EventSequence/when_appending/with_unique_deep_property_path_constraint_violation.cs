// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.with_unique_deep_property_path_constraint_violation.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class with_unique_deep_property_path_constraint_violation(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueMobilePhoneConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(PersonRegistered)];

        public IAppendResult FirstResult { get; private set; }
        public IAppendResult SecondResult { get; private set; }
        PersonRegistered _event;

        public void Establish() => _event = new(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            new("+47", "12345678"));

        public async Task Because()
        {
            FirstResult = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), _event);
            SecondResult = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), _event);
        }
    }

    [Fact] void should_succeed_on_first_attempt() => Context.FirstResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_succeed_on_second_attempt() => Context.SecondResult.IsSuccess.ShouldBeFalse();
}
