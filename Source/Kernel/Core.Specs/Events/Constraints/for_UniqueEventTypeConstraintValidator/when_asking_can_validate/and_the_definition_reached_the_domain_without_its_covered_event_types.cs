// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintValidator.when_asking_can_validate;

/// <summary>
/// The second place a definition persisted before the constraint covered several event types is dereferenced, and
/// the one that matters independently of storage: this runs on every append, not only at registration. Storage
/// upgrades such a definition on read, but the validator must not assume the upgrade happened - if it does, the
/// remedy for a broken store becomes "every append against that constraint throws" rather than "registration
/// throws once".
/// </summary>
public class and_the_definition_reached_the_domain_without_its_covered_event_types : given.a_unique_event_type_constraint_validator
{
    bool _result;
    Exception _error;

    ConstraintValidationContext _context;

    void Establish() => _context = new([], EventSourceId.New(), "SomeEvent", new());

    protected override UniqueEventTypeConstraintDefinition Definition =>
        (UniqueEventTypeConstraintDefinition)RuntimeHelpers.GetUninitializedObject(typeof(UniqueEventTypeConstraintDefinition));

    void Because() => _error = Catch.Exception(() => _result = _validator.CanValidate(_context));

    [Fact] void should_answer_rather_than_throw() => _error.ShouldBeNull();
    [Fact] void should_not_be_able_to_validate() => _result.ShouldBeFalse();
}
