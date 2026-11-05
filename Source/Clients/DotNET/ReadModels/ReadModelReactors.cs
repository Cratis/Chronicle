// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelReactors"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/> the reactors belong to.</param>
/// <param name="clientArtifactsProvider">The <see cref="IClientArtifactsProvider"/> used to discover reactors.</param>
/// <param name="artifactActivator">The <see cref="IClientArtifactsActivator"/> used to activate reactor instances.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to create dependency-resolution scopes.</param>
/// <param name="invoker">The <see cref="IReadModelReactorInvoker"/> used to invoke handler methods.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> used when diffing materialized instances.</param>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
public class ReadModelReactors(
    IEventStore eventStore,
    IClientArtifactsProvider clientArtifactsProvider,
    IClientArtifactsActivator artifactActivator,
    IServiceProvider serviceProvider,
    IReadModelReactorInvoker invoker,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<ReadModelReactors> logger) : IReadModelReactors
{
    readonly object _sync = new();
    readonly List<IDisposable> _subscriptions = [];
    bool _started;
    bool _released;

    /// <inheritdoc/>
    public void Start()
    {
        lock (_sync)
        {
            if (_released || _started)
            {
                return;
            }

            _started = true;
        }

        // Subscribing reaches into the services, so it never runs under the lock.
        var started = new List<IDisposable>();
        try
        {
            foreach (var reactorType in clientArtifactsProvider.ReadModelReactors)
            {
                foreach (var readModelType in ReadModelReactorMethods.GetReadModelTypesFor(reactorType))
                {
                    SubscribeFor(reactorType, readModelType, started);
                }
            }
        }
        finally
        {
            Publish(started);
        }
    }

    /// <inheritdoc/>
    public void Stop() => Dispose();

    /// <inheritdoc/>
    public void Dispose()
    {
        DisposeAll(TakeSubscriptions(release: false));
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the reactors of an event store whose creation failed and that no caller holds. Closes the
    /// subscriptions and makes a <see cref="Start"/> that is still in flight a no-op.
    /// </summary>
    internal void Release() => DisposeAll(TakeSubscriptions(release: true));

    static void DisposeAll(List<IDisposable> subscriptions)
    {
        foreach (var subscription in subscriptions)
        {
            subscription.Dispose();
        }
    }

    static ReadModelChangeType DetermineChangeType<TReadModel>(ReadModelChangeset<TReadModel> changeset, HashSet<string>? seenKeys)
    {
        if (seenKeys is null)
        {
            return changeset.ChangeType;
        }

        var key = changeset.ModelKey.Value;
        if (changeset.Removed)
        {
            seenKeys.Remove(key);
            return ReadModelChangeType.Removed;
        }

        return seenKeys.Add(key) ? ReadModelChangeType.Added : ReadModelChangeType.Modified;
    }

    List<IDisposable> TakeSubscriptions(bool release)
    {
        lock (_sync)
        {
            _released |= release;
            _started = false;
            var taken = _subscriptions.ToList();
            _subscriptions.Clear();
            return taken;
        }
    }

    void Publish(List<IDisposable> started)
    {
        lock (_sync)
        {
            if (!_released && _started)
            {
                _subscriptions.AddRange(started);
                return;
            }
        }

        DisposeAll(started);
    }

    void SubscribeFor(Type reactorType, Type readModelType, List<IDisposable> subscriptions)
    {
        var subscribe = typeof(ReadModelReactors)
            .GetMethod(nameof(Subscribe), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(readModelType);
        subscribe.Invoke(this, [reactorType, subscriptions]);
    }

    void Subscribe<TReadModel>(Type reactorType, List<IDisposable> subscriptions)
    {
        if (Attribute.IsDefined(reactorType, typeof(MaterializedAttribute)))
        {
            subscriptions.Add(SubscribeMaterialized<TReadModel>(reactorType));
            return;
        }

        // Projections report the change type from the server; reducers compute changesets locally without one,
        // so first-seen keys are tracked client-side to distinguish an addition from a modification.
        var seenKeys = eventStore.Reducers.HasFor<TReadModel>() ? new HashSet<string>(StringComparer.Ordinal) : null;

        var subscription = eventStore.ReadModels.Watch<TReadModel>().Subscribe(changeset => DispatchAll(
            reactorType,
            typeof(TReadModel),
            changeset.ReadModel,
            DetermineChangeType(changeset, seenKeys),
            changeset.ChangeContext ?? EventContext.EmptyWithEventSourceId(changeset.ModelKey)));
        subscriptions.Add(subscription);
    }

    IDisposable SubscribeMaterialized<TReadModel>(Type reactorType)
    {
        var differ = new MaterializedReadModelDiffer(jsonSerializerOptions);
        return eventStore.ReadModels.Materialized.ObserveInstances<TReadModel>(take: InstanceCount.Unlimited).Subscribe(
            window =>
            {
                foreach (var change in differ.Diff(window.Cast<object>()))
                {
                    DispatchMaterialized(reactorType, typeof(TReadModel), change.Instance, change.ChangeType, change.ModelKey);
                }
            },

            // Without a handler the failure would be rethrown on whichever thread delivered it.
            error => logger.FailedObservingMaterializedReadModel(typeof(TReadModel).Name, reactorType.Name, error));
    }

    void DispatchMaterialized(Type reactorType, Type readModelType, object? readModel, ReadModelChangeType changeType, string modelKey) =>
        DispatchAll(reactorType, readModelType, readModel, changeType, EventContext.EmptyWithEventSourceId(modelKey));

    void DispatchAll(Type reactorType, Type readModelType, object? readModel, ReadModelChangeType changeType, EventContext changeContext)
    {
        var methods = ReadModelReactorMethods.GetFor(reactorType)
            .Where(_ => _.ReadModelType == readModelType && _.ChangeType == changeType);
        foreach (var method in methods)
        {
            _ = Dispatch(reactorType, method, readModel, changeContext);
        }
    }

    async Task Dispatch(Type reactorType, ReadModelReactorMethod method, object? readModel, EventContext changeContext)
    {
        try
        {
            using var delivery = ObserverDelivery.Enter(
                serviceProvider,
                new(eventStore.Name, eventStore.Namespace, ObserverId.Unspecified, ObserverDeliveryKind.ReadModelReactor, reactorType));
            await using var scope = serviceProvider.CreateAsyncScope();
            var activated = artifactActivator.Activate(scope.ServiceProvider, reactorType);
            if (activated.TryGetException(out var activationException))
            {
                logger.FailedActivatingReadModelReactor(reactorType.Name, activationException);
                return;
            }

            await using var instance = activated.AsT0;
            await invoker.Invoke(eventStore, instance.Instance, method, readModel, changeContext, scope.ServiceProvider);
        }
        catch (Exception ex)
        {
            logger.FailedDispatchingReadModelChange(reactorType.Name, ex);
        }
    }
}
