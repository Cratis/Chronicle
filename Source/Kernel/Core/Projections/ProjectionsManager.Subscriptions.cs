// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections;

public partial class ProjectionsManager
{
    /// <inheritdoc/>
    public Task EnsureSubscribedIn(EventStoreNamespaceName @namespace) => SubscribeInNamespace(@namespace);

    Task SubscribeInNamespace(EventStoreNamespaceName @namespace, bool reconcile = true)
    {
        // A fresh pass (namespace added, activation) subscribes even quarantined observers, which a reconciling
        // pass leaves alone. Joining a pending reconciling pass would lose that, so a fresh pass always runs.
        if (!reconcile)
        {
            return RunNamespaceSubscriptionPass(@namespace, reconcile);
        }

        if (_namespaceSubscriptions.TryGetValue(@namespace, out var pending))
        {
            return pending;
        }

        // Publish the promise before starting work: even synchronously completing dependencies must
        // not leave a completed pass in the map. Explicit callers join, rather than skip, pending work.
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _namespaceSubscriptions.Add(@namespace, completion.Task);
        _ = CompleteNamespaceSubscriptions(@namespace, reconcile, completion);
        return completion.Task;
    }

    async Task CompleteNamespaceSubscriptions(EventStoreNamespaceName @namespace, bool reconcile, TaskCompletionSource completion)
    {
        Exception? failure = null;
        do
        {
            _namespaceReruns.Remove(@namespace);
            try
            {
                await RunNamespaceSubscriptionPass(@namespace, reconcile);
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }
        }
        while (_namespaceReruns.Contains(@namespace));

        // All joiners await requested reruns too. Remove the promise before completing it so a new
        // caller cannot join finished work. A failed pass still honors a retry requested while it ran.
        _namespaceSubscriptions.Remove(@namespace);
        if (failure is null) completion.TrySetResult();
        else completion.TrySetException(failure);
    }

    async Task RunNamespaceSubscriptionPass(EventStoreNamespaceName @namespace, bool reconcile)
    {
        var pass = SubscribeInNamespaceCore(@namespace, reconcile);
        _subscriptionPasses.Add(pass);
        try
        {
            await pass;
        }
        finally
        {
            _subscriptionPasses.Remove(pass);
        }
    }

    async Task SubscribeInNamespaceCore(EventStoreNamespaceName @namespace, bool reconcile)
    {
        var definitions = State.Projections.Where(IsCurrentActiveDefinition).ToArray();
        var missing = new ConcurrentBag<ProjectionDefinition>();
        await Task.WhenAll(definitions.Select(async definition =>
        {
            try
            {
                var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(definition.Identifier, _eventStoreName, @namespace, definition.EventSequenceId));
                if (reconcile && await observer.IsSubscriptionEstablished())
                {
                    _subscribed.TryAdd((definition.Identifier, @namespace), default);
                    return;
                }

                _subscribed.TryRemove((definition.Identifier, @namespace), out _);
                var state = await observer.GetState();

                // A retired observer is left retired - unless a registration meant to revive it and ran out of
                // subscribe budget before it got there.
                var revive = _pendingRevival.ContainsKey((definition.Identifier, @namespace));
                if ((state.AlertDisposition == AlertDisposition.Retired && !revive) || (reconcile && state.RunningState == ObserverRunningState.Quarantined))
                {
                    _subscribed.TryAdd((definition.Identifier, @namespace), default);
                    return;
                }

                missing.Add(definition);
            }
            catch (Exception exception)
            {
                logger.FailedSettingDefinitionAndSubscribing(exception, definition.Identifier);
            }
        }));

        if (!missing.IsEmpty || _failedNamespaceSetups.Contains(@namespace))
        {
            await EnsureEngineNamespace(@namespace);
        }

        if (!missing.IsEmpty)
        {
            await SubscribeMissingInNamespace(@namespace, missing, reconcile);
        }
    }

    async Task EnsureEngineNamespace(EventStoreNamespaceName @namespace)
    {
        try
        {
            // Engine setup must be retried even if subscriptions succeeded on the failed pass.
            await projectionsService.NamespaceAdded(_eventStoreName, @namespace);
            _failedNamespaceSetups.Remove(@namespace);
        }
        catch (Exception exception)
        {
            _failedNamespaceSetups.Add(@namespace);
            logger.FailedEnsuringSubscriptionsInNamespace(exception, @namespace);
        }
    }

    async Task SubscribeMissingInNamespace(EventStoreNamespaceName @namespace, IEnumerable<ProjectionDefinition> definitions, bool reconcile)
    {
        var readModels = (await GrainFactory.GetGrain<IReadModelsManager>(_eventStoreName).GetDefinitions())
            .ToDictionary(readModel => readModel.Identifier);
        var schemas = await storage.GetEventStore(_eventStoreName).EventTypes.GetLatestForAllEventTypes();
        await Task.WhenAll(definitions.Select(async definition =>
        {
            try
            {
                if (!IsCurrentActiveDefinition(definition)) return;

                if (!readModels.TryGetValue(definition.ReadModel, out var readModel))
                {
                    throw new ReadModelNotFound(definition.ReadModel);
                }

                await SetProjectionDefinition(definition);
                // Reviving takes a fresh subscription: a reconciling one cannot end a retirement.
                var revive = _pendingRevival.ContainsKey((definition.Identifier, @namespace));
                await SubscribeIfNotSubscribed(definition, readModel, @namespace, schemas, reactivateRetired: revive, reconcile: reconcile && !revive);
                var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(definition.Identifier, _eventStoreName, @namespace, definition.EventSequenceId));
                if (!await observer.IsSubscriptionEstablished())
                {
                    if (!IsCurrentActiveDefinition(definition)) return;

                    var state = await observer.GetState();
                    if (state.AlertDisposition == AlertDisposition.Retired || state.RunningState == ObserverRunningState.Quarantined)
                    {
                        _subscribed.TryAdd((definition.Identifier, @namespace), default);
                        return;
                    }

                    logger.SubscriptionNotEstablished(definition.Identifier, @namespace);
                }
            }
            catch (Exception exception)
            {
                // An unhealthy artifact must not prevent healthy subscriptions or namespace creation.
                logger.FailedSettingDefinitionAndSubscribing(exception, definition.Identifier);
            }
        }));
    }

    /// <summary>
    /// Repairs registration subscriptions using the activation-local cache. Only namespace-local
    /// triggers check real observer state, so idle tenants do not cause a whole-store observer sweep.
    /// </summary>
    /// <returns>A task representing the subscription repair.</returns>
    async Task ResubscribeProjectionsThatAreNotSubscribed()
    {
        if (_resubscribeRunning)
        {
            _resubscribeRequested = true;
            return;
        }

        _resubscribeRunning = true;
        try
        {
            do
            {
                _resubscribeRequested = false;
                await ResubscribeMissingProjections();
            }
            while (_resubscribeRequested);
        }
        finally
        {
            _resubscribeRunning = false;

            // Even a failed pass cannot consume a request that arrived while it was awaiting storage.
            if (_resubscribeRequested) ScheduleResubscribeProjectionsThatAreNotSubscribed();
        }
    }

    async Task ResubscribeMissingProjections()
    {
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        var activeDefinitions = State.Projections.Where(definition => definition.IsActive).ToArray();
        var missingNamespaces = namespaces.Where(namespaceName => _failedNamespaceSetups.Contains(namespaceName) ||
            activeDefinitions.Any(definition => !_subscribed.ContainsKey((definition.Identifier, namespaceName))));
        await Task.WhenAll(missingNamespaces.Select(async namespaceName =>
        {
            try
            {
                await EnsureSubscribedIn(namespaceName);
            }
            catch (Exception exception)
            {
                // The namespace pass logs individual failures; this scheduled repair has no caller to fail.
                logger.FailedEnsuringSubscriptionsInNamespace(exception, namespaceName);
            }
        }));
    }

    bool IsCurrentActiveDefinition(ProjectionDefinition definition) =>
        !_removingProjections.Contains(definition.Identifier) && definition.IsActive && State.Projections.Contains(definition) && !IsBeingReplaced(definition);

    bool IsBeingReplaced(ProjectionDefinition definition)
    {
        var replaced = _registeringDefinitions.TryGetValue(definition.Identifier, out var registering) && registering != definition;
        if (replaced) _skippedWhileRegistering = true;
        return replaced;
    }

    async Task SetProjectionDefinition(ProjectionDefinition definition)
    {
        // Namespace passes may overlap. Share definition setup across them for this activation,
        // but remove failures so the next trigger retries the artifact.
        var setup = _projectionDefinitions.GetOrAdd(
            definition.Identifier,
            static (_, state) => new Lazy<Task>(() =>
                state.Manager.GrainFactory.GetGrain<IProjection>(new ProjectionKey(state.Definition.Identifier, state.Manager._eventStoreName)).SetDefinition(state.Definition)),
            (Manager: this, Definition: definition));
        try
        {
            await setup.Value;
        }
        catch
        {
            _projectionDefinitions.TryRemove(new KeyValuePair<ProjectionId, Lazy<Task>>(definition.Identifier, setup));
            throw;
        }
    }

    void ForgetSubscriptionsFor(ProjectionId identifier)
    {
        _projectionDefinitions.TryRemove(identifier, out _);
        foreach (var pair in _subscribed.Keys.Where(pair => pair.Projection == identifier))
        {
            _subscribed.TryRemove(pair, out _);
        }

        foreach (var pair in _pendingRevival.Keys.Where(pair => pair.Projection == identifier))
        {
            _pendingRevival.TryRemove(pair, out _);
        }
    }
}
