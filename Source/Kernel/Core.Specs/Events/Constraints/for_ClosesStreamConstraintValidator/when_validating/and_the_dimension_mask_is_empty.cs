// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamConstraintValidator.when_validating;

public class and_the_dimension_mask_is_empty : given.a_property_sourced_closing_constraint
{
    ConstraintValidationResult _result;

    void Establish() => _validator = new(_definition with { Dimensions = ClosedStreamDimensions.None, EventStreamIdFrom = null }, new ClosedStreamsConstraintStorage());

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_refuse_an_unscoped_closure() => _result.IsValid.ShouldBeFalse();
}
