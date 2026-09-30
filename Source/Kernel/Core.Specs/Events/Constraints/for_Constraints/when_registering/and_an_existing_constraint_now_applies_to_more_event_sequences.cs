// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.when_registering;

/// <summary>
/// The rebuild of the newly covered sequences is started here, at registration, so it does not depend on which event
/// sequence grains happen to be active.
/// </summary>
public class and_an_existing_constraint_now_applies_to_more_event_sequences : given.a_constraints_system
{
    UniqueConstraintDefinition _existing;
    UniqueConstraintDefinition _widened;

    void Establish()
    {
        _existing = new UniqueConstraintDefinition("UniqueEmail", [new("InvitationSent", ["email"])])
        {
            EventSequences = [EventSequenceId.Log]
        };
        _widened = _existing with { EventSequences = [] };
        _stateStorage.State.Constraints.Add(_existing);
    }

    async Task Because() => await _constraints.Register([_widened]);

    [Fact]
    void should_rebuild_the_stale_indexes_of_the_event_store_from_the_previous_and_current_definitions() =>
        _constraintIndexes.Received(1).RebuildStaleIndexes(
            (EventStoreName)"Something",
            Arg.Is<IReadOnlyCollection<IConstraintDefinition>>(_ => _.Single().Equals(_existing)),
            Arg.Is<IReadOnlyCollection<IConstraintDefinition>>(_ => _.Single().Equals(_widened)));
}
