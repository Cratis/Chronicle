// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating;

/// <summary>
/// Whether a covered event violates the constraint depends on the removal event as much as on the covered types —
/// an event preceding the most recent release belongs to a closed cycle and blocks nothing. The validator therefore
/// has to hand storage the whole definition; handing it only the covered event types is the shape that type-checks
/// and silently answers the question the constraint is no longer asking.
/// </summary>
public class and_the_constraint_declares_a_removal_event : Specification
{
    UniqueEventTypeConstraintValidator _validator;
    IUniqueEventTypesConstraintsStorage _storage;
    ConstraintValidationContext _context;
    ConstraintValidationResult _result;

    readonly EventType _checkedOutEventType = new("LoanCheckedOut", EventTypeGeneration.First);
    readonly EventType _returnedEventType = new("LoanReturned", EventTypeGeneration.First);

    void Establish()
    {
        _storage = Substitute.For<IUniqueEventTypesConstraintsStorage>();
        var definition = new UniqueEventTypeConstraintDefinition(
            "LoanOpen",
            [_checkedOutEventType.Id],
            [_returnedEventType.Id]);

        _validator = new UniqueEventTypeConstraintValidator(definition, _storage);
        _context = new([], EventSourceId.New(), _checkedOutEventType.Id, new ExpandoObject());

        _storage
            .IsAllowed(Arg.Any<UniqueEventTypeConstraintDefinition>(), Arg.Any<EventSourceId>(), Arg.Any<string>())
            .Returns((true, EventSequenceNumber.Unavailable));
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] async Task should_hand_storage_the_removal_event() =>
        await _storage.Received(1).IsAllowed(
            Arg.Is<UniqueEventTypeConstraintDefinition>(_ => _.RemovedWith.Contains(_returnedEventType.Id)),
            _context.EventSourceId,
            Arg.Any<string>());
}
