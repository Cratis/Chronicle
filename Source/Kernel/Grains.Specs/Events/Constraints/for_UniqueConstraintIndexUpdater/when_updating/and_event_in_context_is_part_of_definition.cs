// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

public class and_event_in_context_is_part_of_definition : Specification
{
    UniqueConstraintIndexUpdater _updater;
    UniqueConstraintDefinition _definition;
    IUniqueConstraintsStorage _storage;
    ConstraintValidationContext _context;
    EventSequenceNumber _eventSequenceNumber;
    UniqueConstraintValue _value;

    void Establish()
    {
        _storage = Substitute.For<IUniqueConstraintsStorage>();
        _definition = new("SomeConstraint", [new("SomeEvent", ["SomeProperty"])]);

        var contentAsExpando = new ExpandoObject();
        dynamic content = contentAsExpando;
        content.SomeProperty = "SomeValue";
        _value = (UniqueConstraintValue)content.SomeProperty;

        _context = new([], EventSourceId.New(), "SomeEvent", contentAsExpando);
        _updater = new(_definition, _context, _storage);

        _eventSequenceNumber = 42L;
    }

    async Task Because() => await _updater.Update(_eventSequenceNumber);

    [Fact] void should_not_save_to_storage() => _storage.Received(1).Save(_context.EventSourceId, _definition.Name, Arg.Any<EventSequenceNumber>(), _value);
}


