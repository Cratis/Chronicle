// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_removing;

public class and_a_silo_lookup_fails : given.a_manager_with_an_orphaned_projection
{
    readonly Exception _failure = new("Silo lookup failed");
    Exception? _error;

    void Establish() => _remoteClients.DeclaresProjection(EventStore, _definition.Identifier).Returns(Task.FromException<bool>(_failure));
    async Task Because() => _error = await Catch.Exception(() => _grain.Remove(_definition.Identifier));

    [Fact] void should_fail_closed() => _error.ShouldBeSame(_failure);
    [Fact] async Task should_not_retire_the_observer() => await _observerGrain.DidNotReceive().Retire();
}
