// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_UniqueConstraintValidator.when_validating;

public class and_value_returned_for_property_is_null : given.a_unique_constraint_validator_with_valid_definition
{
    ConstraintValidationResult _result;

    void Establish() => SetPropertyValue(null!);

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_not_ask_storage_if_allowed() => _storage.DidNotReceive().IsAllowed(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<UniqueConstraintValue>());
}
