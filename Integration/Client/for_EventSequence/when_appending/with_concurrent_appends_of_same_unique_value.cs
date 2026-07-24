// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.with_concurrent_appends_of_same_unique_value.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class with_concurrent_appends_of_same_unique_value(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueUserConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(UserOnboardingStarted), typeof(UserRemoved)];

        public UserOnboardingStarted Event { get; private set; }

        public IReadOnlyList<IAppendResult> Results { get; private set; }

        public void Establish() => Event = new UserOnboardingStarted(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        public async Task Because() =>
            Results = await Task.WhenAll(
                EventStore.EventLog.Append(Guid.NewGuid().ToString(), Event),
                EventStore.EventLog.Append(Guid.NewGuid().ToString(), Event));
    }

    [Fact] void should_have_exactly_one_successful_append() => Context.Results.Count(_ => _.IsSuccess).ShouldEqual(1);
    [Fact] void should_have_exactly_one_append_with_a_constraint_violation() => Context.Results.Count(_ => _.HasConstraintViolations).ShouldEqual(1);
}
