// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_recovering_cataloged_generations;

public class and_generations_are_disabled : given.a_manager_with_a_cataloged_generation
{
    void Establish() => _generations
        .ResolveCataloged(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>())
        .Returns([]);

    Task Because() => _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);

    [Fact] async Task should_not_subscribe_a_generation_observer() => await _generationObserver.DidNotReceiveWithAnyArgs().Subscribe<IProjectionObserverSubscriber>(default, default!, default!);
}
