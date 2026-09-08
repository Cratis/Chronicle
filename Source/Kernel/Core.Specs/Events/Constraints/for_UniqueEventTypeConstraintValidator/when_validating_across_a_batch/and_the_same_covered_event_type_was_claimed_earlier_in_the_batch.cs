// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating_across_a_batch;

/// <summary>
/// The duplicate case of <see cref="and_a_sibling_covered_event_was_claimed_earlier_in_the_batch"/>: the exact same
/// covered event type appended twice for the same event source in one batch must be rejected the second time, the
/// same way it would be rejected across two separate append calls.
/// </summary>
public class and_the_same_covered_event_type_was_claimed_earlier_in_the_batch : Specification
{
    static readonly EventType _checkedOutEventType = new("LoanCheckedOut", 1);
    static readonly EventSourceId _borrower = "borrower-1";

    UniqueEventTypeConstraintValidator _validator;
    ConstraintBatchClaims _batchClaims;
    ConstraintValidationResult _firstResult;
    ConstraintValidationResult _secondResult;

    void Establish()
    {
        var storage = Substitute.For<IUniqueEventTypesConstraintsStorage>();
        storage.IsAllowed(Arg.Any<UniqueEventTypeConstraintDefinition>(), Arg.Any<EventSourceId>(), Arg.Any<ResolvedConstraintScope>())
            .Returns((true, EventSequenceNumber.Unavailable));

        var definition = new UniqueEventTypeConstraintDefinition("loan-open", [_checkedOutEventType.Id]);
        _validator = new(definition, storage);
        _batchClaims = new();
    }

    async Task Because()
    {
        _firstResult = await _validator.Validate(ContextFor());
        _secondResult = await _validator.Validate(ContextFor());
    }

    [Fact] void should_accept_the_first_event() => _firstResult.IsValid.ShouldBeTrue();
    [Fact] void should_reject_the_second_event() => _secondResult.IsValid.ShouldBeFalse();

    ConstraintValidationContext ContextFor() => new([_validator], _borrower, _checkedOutEventType.Id, new ExpandoObject(), batchClaims: _batchClaims);
}
