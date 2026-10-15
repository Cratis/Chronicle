// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

/// <summary>
/// The validator answers Success when there is nothing to constrain, so the index must agree and claim
/// nothing. It used to claim SHA-256("") - one key shared by every such event - so the second one
/// collided in storage on an append the validator had already approved (#4122). The event source still
/// releases whatever it claimed earlier, so the entry is removed rather than left behind (#4383).
/// </summary>
public class and_the_constrained_properties_have_no_value : Specification
{
    UniqueConstraintIndexUpdater _updater;
    UniqueConstraintDefinition _definition;
    IUniqueConstraintsStorage _storage;
    ConstraintValidationContext _context;

    void Establish()
    {
        _storage = Substitute.For<IUniqueConstraintsStorage>();
        _definition = new("SomeConstraint", [new("SomeEvent", ["SomeProperty"])]);

        var contentAsExpando = new ExpandoObject();
        dynamic content = contentAsExpando;
        content.SomeOtherProperty = "SomeValue";

        _context = new([], EventSourceId.New(), "SomeEvent", contentAsExpando);
        _updater = new(_definition, _context, _storage);
    }

    async Task Because() => await _updater.Update(42L);

    [Fact] void should_not_save_anything() => _storage.DidNotReceive().Save(
        Arg.Any<EventSourceId>(),
        Arg.Any<UniqueConstraintDefinition>(),
        Arg.Any<EventSequenceNumber>(),
        Arg.Any<UniqueConstraintValue>(),
        Arg.Any<string>());

    [Fact] void should_remove_the_previous_claim_of_the_event_source() => _storage.Received(1).Remove(_context.EventSourceId, _definition, string.Empty);
}
