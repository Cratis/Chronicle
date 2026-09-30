// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintValidationFactory.when_creating;

/// <summary>
/// A constraint scoped to the event log must not be validated on the outbox - and, since the index is updated by
/// the same validators after a successful append, must not claim anything in the outbox's own index either. That
/// index could only be released by removal events appended to the outbox, so a forwarded fact would otherwise
/// block every later forward of the same value.
/// </summary>
public class and_the_constraints_do_not_apply_to_the_event_sequence : given.a_constraint_validation_factory
{
    IConstraintValidation _result;

    void Establish()
    {
        _definitions.Add(_unique with { EventSequences = [EventSequenceId.Log] });
        _definitions.Add(_uniqueEventType with { EventSequences = [EventSequenceId.Log] });
    }

    async Task Because() => _result = await _factory.Create(KeyFor(EventSequenceId.Outbox));

    [Fact] void should_create_no_validator_for_either() => DefinitionsValidatedBy(_result).ShouldBeEmpty();
}
