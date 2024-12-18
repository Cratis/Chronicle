// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_ConstraintValidationContext.when_updating;

public class and_validators_can_validate_context : given.two_validators_that_are_index_updaters
{
    static readonly EventSequenceNumber _sequenceNumber = 42;
    ConstraintValidationContext _context;

    void Establish()
    {
        _firstValidator.CanValidate(Arg.Any<ConstraintValidationContext>()).Returns(true);
        _secondValidator.CanValidate(Arg.Any<ConstraintValidationContext>()).Returns(true);
        _context = new([_firstValidator, _secondValidator], _eventSourceId, _eventType.Id, _content);
    }

    async Task Because() => await _context.Update(_sequenceNumber);

    [Fact] void should_call_the_first_validator() => _firstValidatorIndexUpdater.Received(1).Update(_sequenceNumber);
    [Fact] void should_call_the_second_validator() => _secondValidatorIndexUpdater.Received(1).Update(_sequenceNumber);
}
