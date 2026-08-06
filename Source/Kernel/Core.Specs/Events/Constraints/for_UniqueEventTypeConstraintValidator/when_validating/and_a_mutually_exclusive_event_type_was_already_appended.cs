// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating;

/// <summary>
/// The invariant CHR-25 asks for: an event source terminal through one outcome cannot then take the other. The
/// validator asks storage about every covered event type, not only the one being appended — asking about just
/// the incoming type would let the second outcome through, since that type itself has not been appended yet.
/// </summary>
public class and_a_mutually_exclusive_event_type_was_already_appended : Specification
{
    UniqueEventTypeConstraintValidator _validator;
    IUniqueEventTypesConstraintsStorage _storage;
    ConstraintValidationContext _context;
    ConstraintValidationResult _result;

    readonly EventType _aliasedEventType = new("PersonAliasedTo", 1);
    readonly EventType _erasedEventType = new("PersonErased", 1);

    void Establish()
    {
        _storage = Substitute.For<IUniqueEventTypesConstraintsStorage>();
        var definition = new UniqueEventTypeConstraintDefinition(
            "PersonTerminal",
            [_aliasedEventType.Id, _erasedEventType.Id]);

        _validator = new UniqueEventTypeConstraintValidator(definition, _storage);
        _context = new([], EventSourceId.New(), _erasedEventType.Id, new ExpandoObject());

        // The person was already merged away — the sibling event type, not the one being appended.
        _storage
            .IsAllowed(Arg.Any<UniqueEventTypeConstraintDefinition>(), Arg.Any<EventSourceId>(), Arg.Any<string>())
            .Returns((false, (EventSequenceNumber)7U));
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_violations() => _result.Violations.ShouldNotBeEmpty();
    [Fact] async Task should_ask_storage_about_every_covered_event_type() =>
        await _storage.Received(1).IsAllowed(
            Arg.Is<UniqueEventTypeConstraintDefinition>(_ => _.EventTypeIds.Contains(_aliasedEventType.Id) && _.EventTypeIds.Contains(_erasedEventType.Id)),
            Arg.Any<EventSourceId>(),
            Arg.Any<string>());
}
