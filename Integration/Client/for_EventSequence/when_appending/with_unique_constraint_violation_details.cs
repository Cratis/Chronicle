// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.with_unique_constraint_violation_details.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class with_unique_constraint_violation_details(context context) : Given<context>(context)
{
    /// <summary>
    /// Mirrors <c>WellKnownConstraintDetailKeys.PropertyName</c>. That constant type is defined
    /// identically in both the client and kernel assemblies referenced by this project, so it cannot be
    /// named directly without an extern alias; the well-known key literals are used instead.
    /// </summary>
    const string PropertyNameDetailKey = "PropertyName";

    /// <summary>
    /// Mirrors <c>WellKnownConstraintDetailKeys.PropertyValue</c> (see <see cref="PropertyNameDetailKey"/>).
    /// </summary>
    const string PropertyValueDetailKey = "PropertyValue";

    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public override IEnumerable<Type> ConstraintTypes => [typeof(UniqueUserConstraint)];
        public override IEnumerable<Type> EventTypes => [typeof(UserOnboardingStarted), typeof(UserRemoved)];

        public UserOnboardingStarted Event { get; private set; }

        public IAppendResult FirstResult { get; private set; }
        public IAppendResult SecondResult { get; private set; }

        public IEnumerable<string> OffendingPropertyNames =>
            SecondResult.ConstraintViolations
                .Where(_ => _.Details.ContainsKey(PropertyNameDetailKey))
                .Select(_ => _.Details[PropertyNameDetailKey].ToLowerInvariant());

        public void Establish() => Event = new UserOnboardingStarted(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        public async Task Because()
        {
            FirstResult = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), Event);
            SecondResult = await EventStore.EventLog.Append(Guid.NewGuid().ToString(), Event);
        }
    }

    [Fact] void should_succeed_on_first_attempt() => Context.FirstResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_succeed_on_second_attempt() => Context.SecondResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_constraint_violations() => Context.SecondResult.ConstraintViolations.ShouldNotBeEmpty();
    [Fact] void should_report_the_unique_constraint_type() => Context.SecondResult.ConstraintViolations.All(_ => _.ConstraintType == ConstraintType.Unique).ShouldBeTrue();
    [Fact] void should_report_the_constraint_name() => Context.SecondResult.ConstraintViolations.All(_ => _.ConstraintName.Value == nameof(UniqueUserConstraint)).ShouldBeTrue();
    [Fact] void should_include_a_property_name_detail_for_every_violation() => Context.SecondResult.ConstraintViolations.All(_ => _.Details.ContainsKey(PropertyNameDetailKey)).ShouldBeTrue();
    [Fact] void should_include_a_non_empty_property_value_detail_for_every_violation() => Context.SecondResult.ConstraintViolations.All(_ => _.Details.TryGetValue(PropertyValueDetailKey, out var value) && !string.IsNullOrEmpty(value)).ShouldBeTrue();
    [Fact] void should_report_the_user_name_as_an_offending_property() => Context.OffendingPropertyNames.ShouldContain(nameof(UserOnboardingStarted.UserName).ToLowerInvariant());
    [Fact] void should_report_the_name_as_an_offending_property() => Context.OffendingPropertyNames.ShouldContain(nameof(UserOnboardingStarted.Name).ToLowerInvariant());
}
