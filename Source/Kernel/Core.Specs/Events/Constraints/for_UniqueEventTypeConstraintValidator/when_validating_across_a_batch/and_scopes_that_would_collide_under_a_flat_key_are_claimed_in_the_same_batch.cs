// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating_across_a_batch;

/// <summary>
/// Two genuinely different scopes must stay different. These two events carry different event source types and
/// different event stream types, but joining the dimensions into one delimited string produces the exact same text
/// for both (<c>est:{sourceType}|estt:{streamType}</c>), so tracking the batch by such a key would let one of them
/// cancel the other's claim. The dimensions are kept typed and compared one by one, so both are admitted.
/// </summary>
public class and_scopes_that_would_collide_under_a_flat_key_are_claimed_in_the_same_batch : Specification
{
    static readonly EventType _checkedOutEventType = new("LoanCheckedOut", 1);
    static readonly EventSourceId _borrower = "borrower-1";

    static readonly EventSourceType _firstSourceType = "loan|estt:branch";
    static readonly EventStreamType _firstStreamType = "region";
    static readonly EventSourceType _secondSourceType = "loan";
    static readonly EventStreamType _secondStreamType = "branch|estt:region";

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
            new ConstraintScope(EventSourceType: (EventSourceType)"_scoped_", EventStreamType: (EventStreamType)"_scoped_"));
        _validator = new(definition, storage);
        _batchClaims = new();
    }

    async Task Because()
    {
        _firstResult = await _validator.Validate(ContextFor(_firstSourceType, _firstStreamType));
        _secondResult = await _validator.Validate(ContextFor(_secondSourceType, _secondStreamType));
    }

    [Fact] void should_accept_the_first_scope() => _firstResult.IsValid.ShouldBeTrue();
    [Fact] void should_accept_the_second_scope() => _secondResult.IsValid.ShouldBeTrue();

    [Fact]
    void should_have_scopes_that_are_indistinguishable_once_flattened() =>
        new ConstraintScope(EventSourceType: (EventSourceType)"_scoped_", EventStreamType: (EventStreamType)"_scoped_")
            .BuildScopeKey(_firstSourceType, _firstStreamType, null)
            .ShouldEqual(new ConstraintScope(EventSourceType: (EventSourceType)"_scoped_", EventStreamType: (EventStreamType)"_scoped_")
                .BuildScopeKey(_secondSourceType, _secondStreamType, null));

    ConstraintValidationContext ContextFor(EventSourceType eventSourceType, EventStreamType eventStreamType) =>
        new(
            [_validator],
            _borrower,
            _checkedOutEventType.Id,
            new ExpandoObject(),
            eventSourceType: eventSourceType,
            eventStreamType: eventStreamType,
            batchClaims: _batchClaims);
}
