// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections;

public partial class ProjectionsManager
{
    /// <inheritdoc/>
    public Task EnsureSubscribedIn(EventStoreNamespaceName @namespace)
    {
        if (_namespaceSubscriptions.TryGetValue(@namespace, out var pending))
        {
            return pending;
        }

        // Publish the promise before starting work: even synchronously completing dependencies must
        // not leave a completed pass in the map. Explicit callers join, rather than skip, pending work.
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _namespaceSubscriptions.Add(@namespace, completion.Task);
        _ = CompleteNamespaceSubscriptions(@namespace, completion);
        return completion.Task;
    }

    async Task CompleteNamespaceSubscriptions(EventStoreNamespaceName @namespace, TaskCompletionSource completion)
    {
        try
        {
            await SubscribeInNamespace(@namespace);
            completion.TrySetResult();
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        finally
        {
            _namespaceSubscriptions.Remove(@namespace);
        }
    }

    async Task SubscribeInNamespace(EventStoreNamespaceName @namespace)
    {
        var definitions = State.Projections.Where(definition => definition.IsActive).ToArray();
        var missing = new ConcurrentBag<ProjectionDefinition>();
        await Task.WhenAll(definitions.Select(async definition =>
        {
            try
            {
                var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(definition.Identifier, _eventStoreName, @namespace, definition.EventSequenceId));
                if (await observer.IsSubscribed())
                {
                    _subscribed.TryAdd((definition.Identifier, @namespace), default);
                    return;
                }

                _subscribed.TryRemove((definition.Identifier, @namespace), out _);
                if ((await observer.GetState()).AlertDisposition == AlertDisposition.Retired)
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

        if (!missing.IsEmpty)
        {
            await SubscribeMissingInNamespace(@namespace, missing);
        }
    }

    async Task SubscribeMissingInNamespace(EventStoreNamespaceName @namespace, IEnumerable<ProjectionDefinition> definitions)
    {
        try
        {
            // Only missing subscriptions need the engine fan-out; healthy watchdog ticks read observer state only.
            await projectionsService.NamespaceAdded(_eventStoreName, @namespace);
        }
        catch (Exception exception)
        {
            // Still attempt every artifact; subsequent namespace triggers retry failed setup.
            logger.FailedEnsuringSubscriptionsInNamespace(exception, @namespace);
        }

        var readModels = (await GrainFactory.GetGrain<IReadModelsManager>(_eventStoreName).GetDefinitions())
            .ToDictionary(readModel => readModel.Identifier);
        var schemas = await storage.GetEventStore(_eventStoreName).EventTypes.GetLatestForAllEventTypes();
        await Task.WhenAll(definitions.Select(async definition =>
        {
            try
            {
                if (!readModels.TryGetValue(definition.ReadModel, out var readModel))
                {
                    throw new ReadModelNotFound(definition.ReadModel);
                }

                await SetProjectionDefinition(definition);
                await SubscribeIfNotSubscribed(definition, readModel, @namespace, schemas, reactivateRetired: false);
                var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(definition.Identifier, _eventStoreName, @namespace, definition.EventSequenceId));
                if (!await observer.IsSubscribed())
                {
                    if ((await observer.GetState()).AlertDisposition == AlertDisposition.Retired)
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
    }
}
