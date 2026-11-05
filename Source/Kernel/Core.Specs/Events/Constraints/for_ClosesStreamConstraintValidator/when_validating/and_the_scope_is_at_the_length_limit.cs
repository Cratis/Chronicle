// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintValidator.when_validating;

public class and_the_scope_is_at_the_length_limit : given.a_property_sourced_closing_constraint
{
    ConstraintValidationResult _result;

    void Establish() => ((IDictionary<string, object?>)_content)["period"] = new string('p', 255);

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_accept_the_append() => _result.IsValid.ShouldBeTrue();
}
