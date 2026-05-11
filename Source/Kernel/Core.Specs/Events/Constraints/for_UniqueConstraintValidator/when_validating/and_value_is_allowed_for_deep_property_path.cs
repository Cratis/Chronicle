// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintValidator.when_validating;

public class and_value_is_allowed_for_deep_property_path : given.a_unique_constraint_validator
{
    static readonly EventTypeId _eventTypeId = "SomeEvent";
    ConstraintValidationContext _context;
    ConstraintValidationResult _result;
    ExpandoObject _content;

    void Establish()
    {
        _content = new();
        dynamic nestedContent = _content;
        nestedContent.Mobile = new ExpandoObject();
        nestedContent.Mobile.UniqueValue = "4711";

        _context = new([], EventSourceId.New(), _eventTypeId, _content);
        _storage.IsAllowed(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<UniqueConstraintValue>()).Returns((true, EventSequenceNumber.First));
    }

    async Task Because() => _result = await _validator.Validate(_context);

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_ask_storage_with_value_from_deep_property_path() => _storage.Received(1).IsAllowed(
        _context.EventSourceId,
        Arg.Any<UniqueConstraintDefinition>(),
        (UniqueConstraintValue)"4711");

    protected override UniqueConstraintDefinition Definition => new("SomeConstraint", [new(_eventTypeId, ["Mobile.UniqueValue"])]);
}
