// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_unchanged_definitions_after_a_namespace_was_added;

public class and_the_new_subscription_is_still_pending : given.a_projections_manager_grain
{
    ProjectionDefinition _definition;
    readonly TaskCompletionSource _subscription = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _registrationCompletedBeforeSubscription;
    bool _subscriptionStarted;

    async Task Establish()
    {
        _definition = CreateDefinition("the-projection", "the-read-model") with { SubscribesToAllEvents = true };
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);

        await _silo.TimerRegistry.FireAllAsync();
        await _grain.Register([_definition]);
        await _silo.TimerRegistry.FireAllAsync();
        _observerGrain.ClearReceivedCalls();
        _namespaces.GetAll().Returns([EventStoreNamespaceName.Default, new EventStoreNamespaceName("new-tenant")]);
        _observerGrain.IsSubscriptionEstablished().Returns(_ => _subscription.Task.IsCompleted);
        _observerGrain.ReconcileSubscription<IProjectionObserverSubscriber>(
                ObserverType.Projection,
                Arg.Any<IEnumerable<Concepts.Events.EventType>>(),
                Arg.Any<SiloAddress>(),
                true)
            .Returns(_ =>
            {
                _subscriptionStarted = true;
                return _subscription.Task;
            });
    }

    async Task Because()
    {
        var registration = _grain.Register([_definition]);
        _registrationCompletedBeforeSubscription = registration.IsCompleted;
        _subscription.TrySetResult();
        await registration;
    }

    [Fact] void should_subscribe_without_waiting_for_the_namespace_notification() => _subscriptionStarted.ShouldBeTrue();
    [Fact] void should_not_report_registration_complete_before_subscription() => _registrationCompletedBeforeSubscription.ShouldBeFalse();
    [Fact] async Task should_not_register_unchanged_definitions_with_the_engine_again() => await _projectionsServiceClient.ReceivedWithAnyArgs(1).Register(default!, default!);
}
