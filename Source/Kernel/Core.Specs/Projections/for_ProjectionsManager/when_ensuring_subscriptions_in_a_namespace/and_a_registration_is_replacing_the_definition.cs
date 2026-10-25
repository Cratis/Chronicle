// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_ensuring_subscriptions_in_a_namespace;

public class and_a_registration_is_replacing_the_definition : given.a_projections_manager_grain
{
    readonly TaskCompletionSource<Result<ProjectionRegistrationError>> _engineRegistration = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ProjectionDefinition _stale;
    ProjectionDefinition _incoming;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _stale = CreateDefinition("the-projection", "the-read-model");
        _incoming = _stale with { IsRewindable = false };
        _state.Projections = [_stale];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _observerGrain.IsSubscriptionEstablished().Returns(false);
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Different);
        _projectionsServiceClient
            .Register(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<ProjectionDefinition>>())
            .Returns(_engineRegistration.Task);
    }

    async Task Because()
    {
        // The registration is held after it has taken the new definition but before it replaced the stored one,
        // so the pass still sees the superseded definition in state.
        var registration = _grain.Register([_incoming]);
        await _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _engineRegistration.TrySetResult(Result<ProjectionRegistrationError>.Success());
        await registration.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_set_the_superseded_definition() => _projectionGrain.DidNotReceive().SetDefinition(_stale);
    [Fact] void should_set_the_registered_definition() => _projectionGrain.Received(1).SetDefinition(_incoming);

    [Fact]
    void should_subscribe_only_for_the_registered_definition() =>
        _observerGrain.Received(1).Subscribe<IProjectionObserverSubscriber>(
            ObserverType.Projection,
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<SiloAddress>(),
            Arg.Any<object?>(),
            Arg.Any<bool>(),
            Arg.Any<ObserverFilters?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>());

    [Fact] void should_store_the_registered_definition() => _state.Projections.ShouldContainOnly(_incoming);
}
