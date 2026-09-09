// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating_across_a_batch;

/// <summary>
/// The in-batch cycle state is keyed by the resolved scope as well as the event source - the same event source
/// claiming the covered event type on two different scoped dimensions within one batch must not interfere with
/// itself. The definition carries the presence marker the client writes for a participating dimension, never a
/// real value, so the values can only come from the events being validated.
/// </summary>
public class and_two_different_scopes_are_claimed_in_the_same_batch : Specification
{
    static readonly EventType _checkedOutEventType = new("LoanCheckedOut", 1);
    static readonly EventSourceId _borrower = "borrower-1";
    static readonly EventStreamId _firstBranch = "branch-1";
    static readonly EventStreamId _secondBranch = "branch-2";

    UniqueEventTypeConstraintValidator _validator;
    ConstraintBatchClaims _batchClaims;
    ConstraintValidationResult _firstBranchResult;
    ConstraintValidationResult _secondBranchResult;

    void Establish()
    {
        var storage = Substitute.For<IUniqueEventTypesConstraintsStorage>();
        storage.IsAllowedWithinScope(Arg.Any<UniqueEventTypeConstraintDefinition>(), Arg.Any<EventSourceId>(), Arg.Any<ResolvedConstraintScope>())
            .Returns((true, EventSequenceNumber.Unavailable));

        var definition = new UniqueEventTypeConstraintDefinition(
            "loan-open",
            [_checkedOutEventType.Id],
            [],
            new ConstraintScope(EventStreamId: (EventStreamId)"_scoped_"));
        _validator = new(definition, storage);
        _batchClaims = new();
    }

    async Task Because()
    {
        _firstBranchResult = await _validator.Validate(ContextFor(_firstBranch));
        _secondBranchResult = await _validator.Validate(ContextFor(_secondBranch));
    }

    [Fact] void should_accept_the_event_on_the_first_branch() => _firstBranchResult.IsValid.ShouldBeTrue();
    [Fact] void should_accept_the_event_on_the_second_branch() => _secondBranchResult.IsValid.ShouldBeTrue();

    ConstraintValidationContext ContextFor(EventStreamId eventStreamId) =>
        new([_validator], _borrower, _checkedOutEventType.Id, new ExpandoObject(), eventStreamId: eventStreamId, batchClaims: _batchClaims);
}
