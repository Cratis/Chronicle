// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating;

public class and_event_type_for_event_source_id_is_allowed : given.a_unique_event_type_constraint_validator_with_valid_definition
{
    ConstraintValidationResult _result;

    void Establish()
    {
        _storage.IsAllowed(_eventType.Id, _context.EventSourceId).Returns((true, EventSequenceNumber.First));
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
}
