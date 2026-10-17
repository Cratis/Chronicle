// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_projection_is_removed_during_setup : given.a_projections_manager_grain
{
    readonly TaskCompletionSource _setup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _settingDefinition = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ProjectionDefinition _definition;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _definition = CreateDefinition("the-projection", "the-read-model") with { SubscribesToAllEvents = true };
        _state.Projections = [_definition];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _observerGrain.IsSubscribed().Returns(false);
        _projectionGrain.SetDefinition(_definition).Returns(_ =>
        {
            _settingDefinition.TrySetResult();
            return _setup.Task;
        });
    }

    async Task Because()
    {
        var pass = _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);
        await _settingDefinition.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _grain.Forget(_definition.Identifier);
        _setup.TrySetResult();
        await pass.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_keep_the_projection_removed() => _state.Projections.ShouldBeEmpty();
    [Fact] async Task should_not_create_an_orphan_subscription() => await _observerGrain.DidNotReceive().SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: Arg.Any<bool>());
}
