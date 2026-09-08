// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating_across_a_batch;

/// <summary>
/// A removal event is never itself validated by this constraint - <see cref="IConstraintValidator.CanValidate"/>
/// only matches the covered event types - so without observing every event in the batch, a removal appended
/// earlier in the same <c>AppendMany</c> call would be invisible to a covered event later in it: durable storage
/// still reports the closed, pre-batch cycle as unresolved. The removal must release the cycle for the rest of the
/// batch the same way it would if it had been appended in an earlier, separate call.
/// </summary>
public class and_a_removal_earlier_in_the_batch_opens_a_new_cycle : Specification
{
    static readonly EventType _startedEventType = new("ShiftStarted", 1);
    static readonly EventType _endedEventType = new("ShiftEnded", 1);
    static readonly EventSourceId _shift = "shift-1";

    UniqueEventTypeConstraintValidator _validator;
    ConstraintBatchClaims _batchClaims;
    ConstraintValidationResult _result;

    void Establish()
    {
        var storage = Substitute.For<IUniqueEventTypesConstraintsStorage>();

        // Durable, pre-batch history still shows an unresolved cycle - the removal below only releases it
        // within the batch, not in storage, which is only updated after the whole batch is durably appended.
        storage.IsAllowed(Arg.Any<UniqueEventTypeConstraintDefinition>(), Arg.Any<EventSourceId>(), Arg.Any<ResolvedConstraintScope>())
            .Returns((false, (EventSequenceNumber)0U));

        var definition = new UniqueEventTypeConstraintDefinition("shift-open", [_startedEventType.Id], [_endedEventType.Id]);
        _validator = new(definition, storage);
        _batchClaims = new();
    }

    async Task Because()
    {
        // The removal event is established (as AppendMany would, in order) but never validated by this
        // constraint - constructing its context is what records the release for the rest of the batch.
        _ = new ConstraintValidationContext([_validator], _shift, _endedEventType.Id, new ExpandoObject(), batchClaims: _batchClaims);

        var startedContext = new ConstraintValidationContext([_validator], _shift, _startedEventType.Id, new ExpandoObject(), batchClaims: _batchClaims);
        _result = await _validator.Validate(startedContext);
    }

    [Fact] void should_accept_the_next_cycle() => _result.IsValid.ShouldBeTrue();
}
