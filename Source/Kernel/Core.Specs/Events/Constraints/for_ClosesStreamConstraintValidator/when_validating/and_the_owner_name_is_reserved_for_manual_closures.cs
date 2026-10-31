// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.InMemory.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintValidator.when_validating;

public class and_the_owner_name_is_reserved_for_manual_closures : given.a_property_sourced_closing_constraint
{
    ConstraintValidationResult _result;

    void Establish()
    {
        ((IDictionary<string, object?>)_content)["period"] = "April";
        _validator = new(_definition with { Name = string.Empty }, new ClosedStreamsConstraintStorage());
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_not_allow_events_to_use_the_manual_owner() => _result.IsValid.ShouldBeFalse();
}
