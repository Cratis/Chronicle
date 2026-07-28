// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintValidator.when_validating;

/// <summary>
/// Characterization of current behavior, which is believed to be wrong - see the assertion remarks.
/// Walks the three events of one batch append through the real validator and a shared
/// <see cref="ConstraintBatchClaims"/>. The persisted index reports every value as free because nothing in the batch
/// has been written yet, so the batch accumulator alone decides the outcome.
/// </summary>
public class and_a_value_released_earlier_in_the_batch_is_claimed : Specification
{
    const string EventTypeId = "SomeEvent";
    const string PropertyName = "SomeProperty";
    static readonly EventSourceId _holder = "first-source";
    static readonly EventSourceId _otherEventSourceId = "second-source";

    UniqueConstraintValidator _validator;
    ConstraintBatchClaims _batchClaims;
    ConstraintValidationResult _holderClaimsReleasedValue;
    ConstraintValidationResult _holderMovesToCurrentValue;
    ConstraintValidationResult _otherClaimsReleasedValue;

    void Establish()
    {
        var storage = Substitute.For<IUniqueConstraintsStorage>();
        storage.IsAllowed(
            Arg.Any<EventSourceId>(),
            Arg.Any<UniqueConstraintDefinition>(),
            Arg.Any<UniqueConstraintValue>(),
            Arg.Any<string>()).Returns((true, EventSequenceNumber.Unavailable));

        _validator = new UniqueConstraintValidator(
            new UniqueConstraintDefinition("some-constraint", [new(EventTypeId, [PropertyName])]),
            storage);

        _batchClaims = new();
    }

    async Task Because()
    {
        _holderClaimsReleasedValue = await _validator.Validate(ContextFor(_holder, "released-value"));
        _holderMovesToCurrentValue = await _validator.Validate(ContextFor(_holder, "current-value"));
        _otherClaimsReleasedValue = await _validator.Validate(ContextFor(_otherEventSourceId, "released-value"));
    }

    [Fact] void should_accept_the_holders_first_value() => _holderClaimsReleasedValue.IsValid.ShouldBeTrue();
    [Fact] void should_accept_the_holder_moving_to_the_second_value() => _holderMovesToCurrentValue.IsValid.ShouldBeTrue();

    /// <summary>
    /// Pins the defect. Once this batch lands the holder owns the current value and the released one is owned by
    /// nobody, so this append is rejected for a unique constraint no stored event will violate. Flip this assertion
    /// to ShouldBeTrue when the batch accumulator is corrected to release a superseded claim.
    /// </summary>
    [Fact] void should_reject_the_second_event_source_claiming_the_released_value() => _otherClaimsReleasedValue.IsValid.ShouldBeFalse();

    ConstraintValidationContext ContextFor(EventSourceId eventSourceId, string value)
    {
        var contentAsExpando = new ExpandoObject();
        dynamic content = contentAsExpando;
        content.SomeProperty = value;
        return new([_validator], eventSourceId, EventTypeId, contentAsExpando, batchClaims: _batchClaims);
    }
}
