// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.given;

public class a_manager_with_a_pass_skipping_an_announced_definition : a_projections_manager_grain
{
    readonly TaskCompletionSource<ProjectionDefinitionCompareResult> _comparison = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<bool> _otherSubscription = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ProjectionDefinition _incoming;
    protected bool _subscribed;
    protected Exception? _registrationFailure;

    async Task Establish()
    {
        await _silo.TimerRegistry.FireAllAsync();
        var stored = CreateDefinition("the-projection", "the-read-model");
        var other = CreateDefinition("other-projection", "the-read-model");
        _incoming = CreateDefinition("the-projection", "the-read-model");
        _state.Projections = [stored, other];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definitionComparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(_comparison.Task);
        _observerGrain.IsSubscriptionEstablished().Returns(_ => _subscribed);
        _observerGrain.ReconcileSubscription<IProjectionObserverSubscriber>(
            ObserverType.Projection, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), false)
            .Returns(_ =>
            {
                _subscribed = true;
                return Task.CompletedTask;
            });
        var otherObserver = Substitute.For<Observation.IObserver>();
        otherObserver.IsSubscriptionEstablished().Returns(_otherSubscription.Task);
        _observersByKey[new ObserverKey(other.Identifier, EventStore, EventStoreNamespaceName.Default, other.EventSequenceId).ToString()] = otherObserver;
    }

    protected async Task RegisterWhilePassIsPending(bool fail)
    {
        // Comparison holds the early announcement in place. This pass skips that definition, then
        // remains in flight on a different observer until after registration requests its retry.
        var registration = _grain.Register([_incoming]);
        var pass = _grain.EnsureSubscribedIn(EventStoreNamespaceName.Default);
        if (fail) _comparison.TrySetException(new InvalidOperationException("Comparison failed"));
        else _comparison.TrySetResult(ProjectionDefinitionCompareResult.Same);
        try
        {
            await registration.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        catch (InvalidOperationException exception)
        {
            _registrationFailure = exception;
        }

        var repair = _silo.TimerRegistry.FireAllAsync();
        _otherSubscription.TrySetResult(true);
        await Task.WhenAll(pass, repair).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }
}
