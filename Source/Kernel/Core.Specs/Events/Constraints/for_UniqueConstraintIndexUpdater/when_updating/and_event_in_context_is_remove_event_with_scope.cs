// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

public class and_event_in_context_is_remove_event_with_scope : Specification
{
    UniqueConstraintIndexUpdater _updater;
    UniqueConstraintDefinition _definition;
    IUniqueConstraintsStorage _storage;
    ConstraintValidationContext _context;

    void Establish()
    {
        _storage = Substitute.For<IUniqueConstraintsStorage>();
        _definition = new("ScopedConstraint", [new("SomeEvent", [])], "SomeRemoveEvent", Scope: new ConstraintScope(EventSourceType: "MySourceType"));

        _context = new([], EventSourceId.New(), "SomeRemoveEvent", new ExpandoObject(), eventSourceType: "MySourceType");
        _updater = new(_definition, _context, _storage);
    }

    async Task Because() => await _updater.Update(EventSequenceNumber.First);

    [Fact] void should_remove_from_storage_with_scope_key() => _storage.Received(1).Remove(_context.EventSourceId, _definition.Name, "est:MySourceType");
}
