// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating;

public class and_event_type_for_event_source_id_is_not_allowed : given.a_unique_event_type_constraint_validator_with_valid_definition
{
    ConstraintValidationResult _result;

    void Establish()
    {
        _storage.IsAllowed(_eventType.Id, _context.EventSourceId).Returns((false, 43U));
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_violations() => _result.Violations.ShouldNotBeEmpty();
    [Fact] void should_have_violation_with_correct_event_type() => _result.Violations[0].EventTypeId.ShouldEqual(_context.EventTypeId);
    [Fact] void should_have_violation_with_correct_event_sequence_number() => _result.Violations[0].SequenceNumber.ShouldEqual(43U);
}
