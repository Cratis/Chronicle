// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintValidator.when_validating;

public class and_property_value_is_missing : given.a_property_sourced_closing_constraint
{
    ConstraintValidationResult _result;

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_refuse_the_append() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_closing_constraint() => _result.Violations.Single().ConstraintType.ShouldEqual(ConstraintType.ClosesStream);
    [Fact] void should_identify_the_missing_property() => _result.Violations.Single().Details[WellKnownConstraintDetailKeys.PropertyName].ShouldEqual("period");
}
