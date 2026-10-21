// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

public class and_mode_is_per_value_and_the_removal_value_is_absent : given.a_per_value_constraint
{
    void Establish()
    {
        _definition = _definition with { RemovalEventDefinitions = [new("removed", ["RemovedId"])] };
        Configure("removed", "Unrelated", "SomeValue");
    }

    async Task Because() => await _updater.Update(42L);

    [Fact] async Task should_not_release_an_empty_hash() => await _storage.DidNotReceive().RemoveValue(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>());
    [Fact] void should_not_release_every_value() => _storage.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == nameof(IUniqueConstraintsStorage.Remove)).ShouldBeFalse();
}
