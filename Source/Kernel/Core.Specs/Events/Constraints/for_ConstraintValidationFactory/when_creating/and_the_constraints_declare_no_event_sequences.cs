// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidationFactory.when_creating;

/// <summary>
/// A constraint that declares no event sequences keeps the behavior every constraint had before it could be narrowed:
/// it is validated on every event sequence, the outbox included.
/// </summary>
public class and_the_constraints_declare_no_event_sequences : given.a_constraint_validation_factory
{
    IConstraintValidation _result;

    void Establish()
    {
        _definitions.Add(_unique);
        _definitions.Add(_uniqueEventType);
    }

    async Task Because() => _result = await _factory.Create(KeyFor(EventSequenceId.Outbox));

    [Fact] void should_validate_and_index_both() => DefinitionsValidatedBy(_result).ShouldContainOnly([_unique, _uniqueEventType]);
}
