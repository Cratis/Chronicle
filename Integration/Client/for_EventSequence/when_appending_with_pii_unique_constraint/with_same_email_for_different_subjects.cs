// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_pii_unique_constraint.with_same_email_for_different_subjects.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_pii_unique_constraint;

[Collection(ChronicleCollection.Name)]
public class with_same_email_for_different_subjects(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueEmailConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(UserRegistered)];

        public string Email { get; private set; }

        public IAppendResult FirstResult { get; private set; }
        public IAppendResult SecondResult { get; private set; }

        public void Establish() => Email = $"{Guid.NewGuid():N}@example.com";

        public async Task Because()
        {
            FirstResult = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), new UserRegistered(Guid.NewGuid().ToString(), Email));
            SecondResult = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), new UserRegistered(Guid.NewGuid().ToString(), Email));
        }
    }

    [Fact] void should_succeed_on_first_attempt() => Context.FirstResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_succeed_on_second_attempt() => Context.SecondResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_a_unique_constraint_violation_on_second_attempt() => Context.SecondResult.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_report_the_unique_email_constraint_by_name() => Context.SecondResult.ConstraintViolations.All(_ => _.ConstraintName.Value == nameof(UniqueEmailConstraint)).ShouldBeTrue();
}
