// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating_across_a_batch;

/// <summary>
/// A removal only ends the cycle it belongs to. Returning a loan at one branch must not reopen the cycle held at
/// another, so a removal earlier in the batch carrying a different scope leaves the claim standing.
/// </summary>
public class and_a_removal_in_a_different_scope_is_earlier_in_the_batch : Specification
{
    static readonly EventType _checkedOutEventType = new("LoanCheckedOut", 1);
    static readonly EventType _returnedEventType = new("LoanReturned", 1);
    static readonly EventSourceId _borrower = "borrower-1";
    static readonly EventStreamId _branch = "branch-1";
    static readonly EventStreamId _otherBranch = "branch-2";

    UniqueEventTypeConstraintValidator _validator;
    ConstraintBatchClaims _batchClaims;
    ConstraintValidationResult _firstResult;
    ConstraintValidationResult _secondResult;

    void Establish()
    {
        var storage = Substitute.For<IUniqueEventTypesConstraintsStorage>();
        storage.IsAllowedWithinScope(Arg.Any<UniqueEventTypeConstraintDefinition>(), Arg.Any<EventSourceId>(), Arg.Any<ResolvedConstraintScope>())
            .Returns((true, EventSequenceNumber.Unavailable));

        var definition = new UniqueEventTypeConstraintDefinition(
            "loan-open",
            [_checkedOutEventType.Id],
            [_returnedEventType.Id],
            new ConstraintScope(EventStreamId: (EventStreamId)"_scoped_"));
        _validator = new(definition, storage);
        _batchClaims = new();
    }

    async Task Because()
    {
        _firstResult = await ContextFor(_checkedOutEventType, _branch).Validate();

        // AppendMany validates the removal's context even though this constraint does not cover it.
        await ContextFor(_returnedEventType, _otherBranch).Validate();

        _secondResult = await ContextFor(_checkedOutEventType, _branch).Validate();
    }

    [Fact] void should_accept_the_covered_event_that_opens_the_cycle() => _firstResult.IsValid.ShouldBeTrue();
    [Fact] void should_still_reject_the_next_covered_event_in_the_scope_that_was_not_released() => _secondResult.IsValid.ShouldBeFalse();

    ConstraintValidationContext ContextFor(EventType eventType, EventStreamId eventStreamId) =>
        new([_validator], _borrower, eventType.Id, new ExpandoObject(), eventStreamId: eventStreamId, batchClaims: _batchClaims);
}
