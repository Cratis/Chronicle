// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintValidator.when_validating;

/// <summary>
/// The offending value stays out of the message, so the details are the only place an application can reach it -
/// and a documented WithMessage callback renders exactly this key to name the value the caller just tried to use.
/// </summary>
/// <remarks>
/// The detail used to carry the value the index is keyed on: a SHA-256 hash of every constrained property
/// concatenated, computed to keep the value itself out of the index. So the documented callback rendered 64
/// characters of hex, and on a multi-property constraint the per-property detail was not per property either.
/// </remarks>
public class and_the_violation_details_are_read : given.a_unique_constraint_validator_with_valid_definition
{
    const string OffendingValue = "the-offending-value";

    ConstraintValidationResult _result;

    void Establish()
    {
        SetPropertyValue(OffendingValue);
        _storage.IsAllowed(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<UniqueConstraintValue>()).Returns((false, 43U));
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_name_the_offending_property() => _result.Violations[0].Details[WellKnownConstraintDetailKeys.PropertyName].ShouldEqual(Property);
    [Fact] void should_carry_the_offending_value() => _result.Violations[0].Details[WellKnownConstraintDetailKeys.PropertyValue].ShouldEqual(OffendingValue);
}
