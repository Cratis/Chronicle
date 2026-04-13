// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_validating;

public class and_event_type_for_event_source_id_is_not_allowed_with_scope : Specification
{
    UniqueEventTypeConstraintValidator _validator;
    IUniqueEventTypesConstraintsStorage _storage;
    ConstraintValidationContext _context;
    ConstraintValidationResult _result;

    void Establish()
    {
        _storage = Substitute.For<IUniqueEventTypesConstraintsStorage>();
        var eventType = new EventType("SomeEvent", 1);
        var definition = new UniqueEventTypeConstraintDefinition(
            "ScopedConstraint",
            eventType.Id,
            Scope: new ConstraintScope(EventSourceType: "MySourceType"));

        _validator = new UniqueEventTypeConstraintValidator(definition, _storage);
        _context = new([], EventSourceId.New(), eventType.Id, new ExpandoObject(), eventSourceType: "MySourceType");

        _storage.IsAllowed(eventType.Id, _context.EventSourceId, "est:MySourceType").Returns((false, 42U));
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_violations() => _result.Violations.ShouldNotBeEmpty();
    [Fact] void should_pass_scope_key_to_storage() => _storage.Received(1).IsAllowed(Arg.Any<EventTypeId>(), Arg.Any<EventSourceId>(), "est:MySourceType");
}
