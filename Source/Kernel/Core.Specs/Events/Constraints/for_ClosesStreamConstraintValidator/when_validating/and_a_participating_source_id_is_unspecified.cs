// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintValidator.when_validating;

public class and_a_participating_source_id_is_unspecified : given.a_property_sourced_closing_constraint
{
    ConstraintValidationResult _result;

    void Establish()
    {
        ((IDictionary<string, object?>)_content)["period"] = "April";
        _context = new([], EventSourceId.Unspecified, "Closed", _content, EventSourceType.Default, EventStreamType.All, EventStreamId.Default);
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_not_widen_to_a_stream_identifier_only_scope() => _result.IsValid.ShouldBeFalse();
}
