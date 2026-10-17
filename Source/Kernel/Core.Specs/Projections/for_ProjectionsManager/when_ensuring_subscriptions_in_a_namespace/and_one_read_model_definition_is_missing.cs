// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_ensuring_subscriptions_in_a_namespace;

public class and_one_read_model_definition_is_missing : given.a_projections_manager_grain
{
    ProjectionDefinition _healthy;
    Exception _exception;
    bool _subscribed;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _healthy = CreateDefinition("healthy-projection", "the-read-model") with { SubscribesToAllEvents = true };
        _state.Projections = [CreateDefinition("broken-projection", "missing-read-model"), _healthy];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _observerGrain.IsSubscribed().Returns(_ => _subscribed);
        _observerGrain.SubscribeToAllEvents<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<SiloAddress>(), reactivateRetired: false)
            .Returns(_ =>
            {
                _subscribed = true;
                return Task.CompletedTask;
            });
    }

    async Task Because() => _exception = await Catch.Exception(() => _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default));

    [Fact] void should_not_fail_the_namespace_pass() => _exception.ShouldBeNull();
    [Fact] void should_log_the_missing_read_model() => _logger.ReceivedCalls().Any(call => call.GetArguments().Any(argument => argument is ReadModelNotFound)).ShouldBeTrue();
    [Fact] async Task should_set_up_the_healthy_projection() => await _projectionGrain.Received(1).SetDefinition(_healthy);
    [Fact] void should_subscribe_the_healthy_projection() => _subscribed.ShouldBeTrue();
}
