// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.with_unique_constraint_ignoring_casing_violated_by_different_casing.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class with_unique_constraint_ignoring_casing_violated_by_different_casing(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueOrganizationNameIgnoringCasingConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(OrganizationNameClaimed)];

        public IAppendResult FirstResult { get; private set; }
        public IAppendResult SecondResult { get; private set; }

        public async Task Because()
        {
            FirstResult = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), new OrganizationNameClaimed("ACME"));
            SecondResult = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), new OrganizationNameClaimed("Acme"));
        }
    }

    [Fact] void should_succeed_on_first_attempt() => Context.FirstResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_succeed_on_second_attempt() => Context.SecondResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_a_constraint_violation() => Context.SecondResult.ConstraintViolations.ShouldNotBeEmpty();
}
