// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating_across_a_batch;

/// <summary>
/// AppendMany validates every event in a batch against durable, pre-batch history before any of them are written,
/// so two mutually exclusive covered event types for the same event source in one batch would otherwise both be
/// admitted - durable storage reports both as allowed because neither has actually been appended yet.
/// </summary>
public class and_a_sibling_covered_event_was_claimed_earlier_in_the_batch : Specification
{
    static readonly EventType _cancelledEventType = new("OrderCancelled", 1);
    static readonly EventType _completedEventType = new("OrderCompleted", 1);
    static readonly EventSourceId _orderId = "order-1";

    UniqueEventTypeConstraintValidator _validator;
    ConstraintBatchClaims _batchClaims;
    ConstraintValidationResult _firstResult;
    ConstraintValidationResult _secondResult;

    void Establish()
    {
        var storage = Substitute.For<IUniqueEventTypesConstraintsStorage>();
        storage.IsAllowed(Arg.Any<UniqueEventTypeConstraintDefinition>(), Arg.Any<EventSourceId>(), Arg.Any<ResolvedConstraintScope>())
            .Returns((true, EventSequenceNumber.Unavailable));

        var definition = new UniqueEventTypeConstraintDefinition("order-terminal", [_cancelledEventType.Id, _completedEventType.Id]);
        _validator = new(definition, storage);
        _batchClaims = new();
    }

    async Task Because()
    {
        _firstResult = await _validator.Validate(ContextFor(_cancelledEventType));
        _secondResult = await _validator.Validate(ContextFor(_completedEventType));
    }

    [Fact] void should_accept_the_first_covered_event() => _firstResult.IsValid.ShouldBeTrue();
    [Fact] void should_reject_the_second_covered_event() => _secondResult.IsValid.ShouldBeFalse();

    ConstraintValidationContext ContextFor(EventType eventType) => new([_validator], _orderId, eventType.Id, new ExpandoObject(), batchClaims: _batchClaims);
}
