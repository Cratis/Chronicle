// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating_across_a_batch;

/// <summary>
/// The natural single-batch expression of one lifecycle transition: a covered event starting a cycle and its
/// removal ending it, appended together. Both must be admitted, and the removal must still release the claim for
/// whatever comes after it in the batch.
/// </summary>
public class and_a_covered_event_is_followed_by_its_removal_in_the_batch : Specification
{
    static readonly EventType _startedEventType = new("ShiftStarted", 1);
    static readonly EventType _endedEventType = new("ShiftEnded", 1);
    static readonly EventSourceId _shift = "shift-1";

    UniqueEventTypeConstraintValidator _validator;
    ConstraintBatchClaims _batchClaims;
    ConstraintValidationResult _startedResult;
    ConstraintValidationResult _nextCycleResult;

    void Establish()
    {
        var storage = Substitute.For<IUniqueEventTypesConstraintsStorage>();
        storage.IsAllowed(Arg.Any<UniqueEventTypeConstraintDefinition>(), Arg.Any<EventSourceId>(), Arg.Any<ResolvedConstraintScope>())
            .Returns((true, EventSequenceNumber.Unavailable));

        var definition = new UniqueEventTypeConstraintDefinition("shift-open", [_startedEventType.Id], [_endedEventType.Id]);
        _validator = new(definition, storage);
        _batchClaims = new();
    }

    async Task Because()
    {
        _startedResult = await _validator.Validate(new([_validator], _shift, _startedEventType.Id, new ExpandoObject(), batchClaims: _batchClaims));

        // The removal is never itself validated by this constraint - establishing its context is what
        // records the release, exactly as AppendMany would do while iterating the batch in order.
        _ = new ConstraintValidationContext([_validator], _shift, _endedEventType.Id, new ExpandoObject(), batchClaims: _batchClaims);

        _nextCycleResult = await _validator.Validate(new([_validator], _shift, _startedEventType.Id, new ExpandoObject(), batchClaims: _batchClaims));
    }

    [Fact] void should_accept_the_covered_event_that_opens_the_cycle() => _startedResult.IsValid.ShouldBeTrue();
    [Fact] void should_accept_a_further_covered_event_after_the_removal_released_it() => _nextCycleResult.IsValid.ShouldBeTrue();
}
