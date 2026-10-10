// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

public class and_mode_is_per_value_and_remove_event_carries_no_properties : given.a_per_value_constraint
{
    void Establish() => Configure("removed", "Unrelated", "SomeValue");

    async Task Because() => await _updater.Update(42L);

    [Fact] async Task should_release_every_value() => await _storage.Received(1).Remove(_owner, _definition, string.Empty);
    [Fact] async Task should_not_release_a_single_value() => await _storage.DidNotReceive().RemoveValue(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>());
}
