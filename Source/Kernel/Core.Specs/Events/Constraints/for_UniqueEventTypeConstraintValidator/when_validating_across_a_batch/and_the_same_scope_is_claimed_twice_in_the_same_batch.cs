// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating_across_a_batch;

/// <summary>
/// Scoping narrows the constraint, it does not weaken it: two covered events for the same event source within the
/// same scope in one batch must be rejected the second time, exactly as an unscoped constraint would.
/// </summary>
public class and_the_same_scope_is_claimed_twice_in_the_same_batch : Specification
{
    static readonly EventType _checkedOutEventType = new("LoanCheckedOut", 1);
    static readonly EventSourceId _borrower = "borrower-1";
    static readonly EventStreamId _branch = "branch-1";

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
            [],
            new ConstraintScope(EventStreamId: (EventStreamId)"_scoped_"));
        _validator = new(definition, storage);
        _batchClaims = new();
    }

    async Task Because()
    {
        _firstResult = await _validator.Validate(ContextFor());
        _secondResult = await _validator.Validate(ContextFor());
    }

    [Fact] void should_accept_the_first_event() => _firstResult.IsValid.ShouldBeTrue();
    [Fact] void should_reject_the_second_event_in_the_same_scope() => _secondResult.IsValid.ShouldBeFalse();

    ConstraintValidationContext ContextFor() =>
        new([_validator], _borrower, _checkedOutEventType.Id, new ExpandoObject(), eventStreamId: _branch, batchClaims: _batchClaims);
}
