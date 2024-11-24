// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_ConstraintValidationContext;

public class when_updating : given.a_constraint_validation_context_with_two_validators
{
    static readonly EventSequenceNumber _sequenceNumber = 42;

    async Task Because() => await _context.Update(_sequenceNumber);

    [Fact] void should_call_the_first_validator() => _firstValidator.Received(1).Update(_context, _sequenceNumber);
    [Fact] void should_call_the_second_validator() => _secondValidator.Received(1).Update(_context, _sequenceNumber);
}
