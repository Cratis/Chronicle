// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending.with_unique_deep_property_path_concatenation_without_violation.context;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class with_unique_deep_property_path_concatenation_without_violation(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueMobilePhoneConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(PersonRegistered)];

        public IAppendResult FirstResult { get; private set; }
        public IAppendResult SecondResult { get; private set; }
        PersonRegistered _firstEvent;
        PersonRegistered _secondEvent;

        void Establish()
        {
            _firstEvent = new(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                new("+47", "12345678"));
            _secondEvent = new(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                new("+47", "12345679"));
        }

        async Task Because()
        {
            FirstResult = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), _firstEvent);
            SecondResult = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), _secondEvent);
        }
    }

    [Fact] void should_succeed_on_first_attempt() => Context.FirstResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_on_second_attempt() => Context.SecondResult.IsSuccess.ShouldBeTrue();
}
