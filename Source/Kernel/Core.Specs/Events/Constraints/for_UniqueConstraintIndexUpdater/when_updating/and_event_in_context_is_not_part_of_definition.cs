// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

public class and_event_in_context_is_not_part_of_definition : Specification
{
    UniqueConstraintIndexUpdater _updater;
    UniqueConstraintDefinition _definition;
    IUniqueConstraintsStorage _storage;
    ConstraintValidationContext _context;

    void Establish()
    {
        _storage = Substitute.For<IUniqueConstraintsStorage>();
        _definition = new("SomeConstraint", [new("SomeEvent", [])]);

        _context = new([], EventSourceId.New(), "SomeOtherEvent", new ExpandoObject());
        _updater = new(_definition, _context, _storage);
    }

    async Task Because() => await _updater.Update(EventSequenceNumber.First);

    [Fact] void should_not_save_to_storage() => _storage.DidNotReceive().Save(Arg.Any<EventSourceId>(), Arg.Any<ConstraintName>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>());
}
