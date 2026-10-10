// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintValidator.when_validating;

public class and_property_value_is_null : given.a_property_sourced_closing_constraint
{
    ConstraintValidationResult _result;

    void Establish() => ((IDictionary<string, object?>)_content)["period"] = null;

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_refuse_the_append() => _result.IsValid.ShouldBeFalse();
}
