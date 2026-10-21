// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

public class and_the_constrained_property_is_personal_data : Specification
{
    UniqueConstraintIndexUpdater _updater;
    UniqueConstraintDefinition _definition;
    IUniqueConstraintsStorage _storage;
    ConstraintValidationContext _context;
    Subject _subject;

    void Establish()
    {
        _storage = Substitute.For<IUniqueConstraintsStorage>();
        _definition = new("SomeConstraint", [new("SomeEvent", ["Email"])]);
        _subject = "the-person";

        var content = new ExpandoObject();
        ((dynamic)content).Email = "someone@example.com";

        _context = new([], EventSourceId.New(), "SomeEvent", content)
        {
            Subject = _subject,
            PersonalProperties = ["email"]
        };
        _updater = new(_definition, _context, _storage);
    }

    async Task Because() => await _updater.Update(42L);

    [Fact] void should_record_the_subject_on_the_claim() =>
        _storage.Received(1).Save(_context.EventSourceId, _definition, Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), _subject, Arg.Any<CancellationToken>());
}
