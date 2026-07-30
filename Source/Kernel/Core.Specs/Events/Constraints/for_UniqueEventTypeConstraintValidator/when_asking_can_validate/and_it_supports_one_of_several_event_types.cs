// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_asking_can_validate;

/// <summary>
/// A constraint covering several event types validates every one of them — that is what makes them mutually
/// exclusive rather than independently unique.
/// </summary>
public class and_it_supports_one_of_several_event_types : given.a_unique_event_type_constraint_validator
{
    bool _result;

    ConstraintValidationContext _context;

    EventType _erasedEventType = new("PersonErased", 1);

    void Establish() => _context = new([], EventSourceId.New(), _erasedEventType.Id, new());

    protected override UniqueEventTypeConstraintDefinition Definition =>
        new("PersonTerminal", [(EventTypeId)"PersonAliasedTo", _erasedEventType.Id]);

    void Because() => _result = _validator.CanValidate(_context);

    [Fact] void should_be_able_to_validate() => _result.ShouldBeTrue();
}
