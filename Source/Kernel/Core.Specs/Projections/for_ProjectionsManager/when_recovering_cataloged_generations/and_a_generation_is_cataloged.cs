// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_recovering_cataloged_generations;

public class and_a_generation_is_cataloged : given.a_manager_with_a_cataloged_generation
{
    Task Because() => _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);

    [Fact] async Task should_resolve_the_cataloged_generations() => await _generations.Received().ResolveCataloged(EventStore, EventStoreNamespaceName.Default, _definition.Identifier);
    [Fact] async Task should_subscribe_the_generation_observer() => await _generationObserver.ReceivedWithAnyArgs(1).Subscribe<IProjectionObserverSubscriber>(default, default!, default!);
    [Fact] async Task should_not_subscribe_the_generation_observer_to_all_events() => await _generationObserver.DidNotReceiveWithAnyArgs().SubscribeToAllEvents<IProjectionObserverSubscriber>(default, default!);
}
