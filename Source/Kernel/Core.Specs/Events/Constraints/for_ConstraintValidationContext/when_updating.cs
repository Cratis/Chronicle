// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidationContext;

public class when_updating : given.a_constraint_validation_context_with_two_validators_that_are_index_updaters
{
    static readonly EventSequenceNumber _sequenceNumber = 42;

    async Task Because() => await _context.Update(_sequenceNumber);

    [Fact] void should_call_the_first_validator() => _firstValidatorIndexUpdater.Received(1).Update(_sequenceNumber);
    [Fact] void should_call_the_second_validator() => _secondValidatorIndexUpdater.Received(1).Update(_sequenceNumber);
}
