// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintValidator.when_validating;

/// <summary>
/// Characterization of current behavior. The same release happens inside the batch as in
/// <see cref="and_a_value_released_earlier_in_the_batch_is_claimed"/>, but the holder's original claim was persisted
/// by an earlier append rather than made in this batch. The rejection then comes from the persisted index read,
/// which no change to <see cref="ConstraintBatchClaims"/> can reach - the batch accumulator is only consulted when
/// storage has already allowed the value.
/// </summary>
public class and_a_value_released_earlier_in_the_batch_was_claimed_before_the_batch : Specification
{
    const string EventTypeId = "SomeEvent";
    const string PropertyName = "SomeProperty";
    const string ReleasedValue = "released-value";
    static readonly EventSourceId _holder = "first-source";
    static readonly EventSourceId _otherEventSourceId = "second-source";

    /// <summary>
    /// Storage never sees the raw value - the validator hands it the SHA-256 hash of the constrained properties.
    /// </summary>
    static readonly UniqueConstraintValue _releasedValueHash =
        new List<UniqueConstraintPropertyAndValue> { new(PropertyName, ReleasedValue) }.GetValue();

    UniqueConstraintValidator _validator;
    ConstraintBatchClaims _batchClaims;
    ConstraintValidationResult _holderMovesToCurrentValue;
    ConstraintValidationResult _otherClaimsReleasedValue;
    bool _releasedValueWasNeverRecordedAsAClaim;

    void Establish()
    {
        var storage = Substitute.For<IUniqueConstraintsStorage>();
        storage.IsAllowed(
            Arg.Any<EventSourceId>(),
            Arg.Any<UniqueConstraintDefinition>(),
            Arg.Any<UniqueConstraintValue>(),
            Arg.Any<string>()).Returns((true, EventSequenceNumber.Unavailable));

        // The holder claimed the released value in an earlier append, so the persisted index still reports it taken
        // for anyone else - the batch's own index updates have not been applied yet.
        storage.IsAllowed(
            Arg.Is<EventSourceId>(eventSourceId => eventSourceId != _holder),
            Arg.Any<UniqueConstraintDefinition>(),
            Arg.Is<UniqueConstraintValue>(value => value == _releasedValueHash),
            Arg.Any<string>()).Returns((false, (EventSequenceNumber)7UL));

        _validator = new UniqueConstraintValidator(
            new UniqueConstraintDefinition("some-constraint", [new(EventTypeId, [PropertyName])]),
            storage);

        _batchClaims = new();
    }

    async Task Because()
    {
        _holderMovesToCurrentValue = await _validator.Validate(ContextFor(_holder, "current-value"));
        _otherClaimsReleasedValue = await _validator.Validate(ContextFor(_otherEventSourceId, ReleasedValue));
        _releasedValueWasNeverRecordedAsAClaim = _batchClaims.TryClaim("some-constraint", string.Empty, _releasedValueHash, "third-source");
    }

    [Fact] void should_accept_the_holder_moving_to_the_current_value() => _holderMovesToCurrentValue.IsValid.ShouldBeTrue();
    [Fact] void should_reject_the_second_event_source_claiming_the_released_value() => _otherClaimsReleasedValue.IsValid.ShouldBeFalse();

    /// <summary>
    /// Shows where the rejection came from. The batch accumulator never saw the released value, because
    /// <see cref="UniqueConstraintValidator"/> only consults it after storage has allowed the value - so a third
    /// event source can still claim it there. Correcting the accumulator alone leaves this rejection in place.
    /// </summary>
    [Fact] void should_not_have_consulted_the_batch_claims() => _releasedValueWasNeverRecordedAsAClaim.ShouldBeTrue();

    ConstraintValidationContext ContextFor(EventSourceId eventSourceId, string value)
    {
        var contentAsExpando = new ExpandoObject();
        dynamic content = contentAsExpando;
        content.SomeProperty = value;
        return new([_validator], eventSourceId, EventTypeId, contentAsExpando, batchClaims: _batchClaims);
    }
}
