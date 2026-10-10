// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

public class and_mode_is_per_value_and_remove_event_carries_the_value : given.a_per_value_constraint
{
    void Establish()
    {
        _definition = _definition with { RemovalEventDefinitions = [new("removed", ["RemovedId"])] };
        Configure("removed", "RemovedId", "SomeValue");
    }

    async Task Because() => await _updater.Update(42L);

    [Fact] async Task should_release_only_the_carried_value() => await _storage.Received(1).RemoveValue(_owner, _definition, "4f7aa54b8d9a8f5e7b06bf38217a84dfd7272bd50f5aebe97ae321f24eceb291", string.Empty);
    [Fact] void should_not_release_every_value() => _storage.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == nameof(IUniqueConstraintsStorage.Remove)).ShouldBeFalse();
}
