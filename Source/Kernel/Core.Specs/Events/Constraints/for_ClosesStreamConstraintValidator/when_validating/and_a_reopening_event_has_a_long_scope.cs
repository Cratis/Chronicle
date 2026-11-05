// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintValidator.when_validating;

public class and_a_reopening_event_has_a_long_scope : given.a_property_sourced_closing_constraint
{
    ConstraintValidationResult _result;

    void Establish()
    {
        ((IDictionary<string, object?>)_content)["period"] = new string('p', 300);
        _context = new([], "source", "Reopened", _content, EventSourceType.Default, EventStreamType.All, EventStreamId.Default);
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_accept_the_append() => _result.IsValid.ShouldBeTrue();
}
