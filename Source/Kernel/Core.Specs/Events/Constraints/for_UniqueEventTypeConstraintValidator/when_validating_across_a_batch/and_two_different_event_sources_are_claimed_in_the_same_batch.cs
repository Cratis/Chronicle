// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating_across_a_batch;

/// <summary>
/// The in-batch cycle state is keyed by event source (and scope), not by constraint alone - two different event
/// sources claiming the same covered event type in one batch must not interfere with each other.
/// </summary>
public class and_two_different_event_sources_are_claimed_in_the_same_batch : Specification
{
    static readonly EventType _checkedOutEventType = new("LoanCheckedOut", 1);
    static readonly EventSourceId _firstBorrower = "borrower-1";
    static readonly EventSourceId _secondBorrower = "borrower-2";

    UniqueEventTypeConstraintValidator _validator;
    ConstraintBatchClaims _batchClaims;
    ConstraintValidationResult _firstBorrowerResult;
    ConstraintValidationResult _secondBorrowerResult;

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
        _firstBorrowerResult = await _validator.Validate(ContextFor(_firstBorrower));
        _secondBorrowerResult = await _validator.Validate(ContextFor(_secondBorrower));
    }

    [Fact] void should_accept_the_first_borrowers_event() => _firstBorrowerResult.IsValid.ShouldBeTrue();
    [Fact] void should_accept_the_second_borrowers_event() => _secondBorrowerResult.IsValid.ShouldBeTrue();

    ConstraintValidationContext ContextFor(EventSourceId eventSourceId) => new([_validator], eventSourceId, _checkedOutEventType.Id, new ExpandoObject(), batchClaims: _batchClaims);
}
