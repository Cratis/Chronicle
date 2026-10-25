// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_recovering_cataloged_generations;

public class and_resolving_them_fails : given.a_manager_with_a_cataloged_generation
{
    Exception _exception;

    void Establish() => _generations
        .ResolveCataloged(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>())
        .ThrowsAsync(new InvalidMaterializationExecution());

    async Task Because() => _exception = await Catch.Exception(() => _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default));

    [Fact] void should_not_fail_the_namespace_pass() => _exception.ShouldBeNull();
    [Fact] async Task should_not_subscribe_a_generation_observer() => await _generationObserver.DidNotReceiveWithAnyArgs().Subscribe<IProjectionObserverSubscriber>(default, default!, default!);
}
