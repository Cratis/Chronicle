// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Events;
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
    readonly List<IDisposable> _subscriptions = [];
    bool _started;

    /// <inheritdoc/>
    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        foreach (var reactorType in clientArtifactsProvider.ReadModelReactors)
        {
            foreach (var readModelType in ReadModelReactorMethods.GetReadModelTypesFor(reactorType))
            {
                SubscribeFor(reactorType, readModelType);
            }
        }
    }

    /// <inheritdoc/>
    public void Stop() => Dispose();

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        _started = false;
        GC.SuppressFinalize(this);
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

    void SubscribeFor(Type reactorType, Type readModelType)
    {
        var subscribe = typeof(ReadModelReactors)
            .GetMethod(nameof(Subscribe), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(readModelType);
        subscribe.Invoke(this, [reactorType]);
    }

    void Subscribe<TReadModel>(Type reactorType)
    {
        if (Attribute.IsDefined(reactorType, typeof(MaterializedAttribute)))
        {
            _subscriptions.Add(SubscribeMaterialized<TReadModel>(reactorType));
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
        _subscriptions.Add(subscription);
    }

    IDisposable SubscribeMaterialized<TReadModel>(Type reactorType)
    {
        var differ = new MaterializedReadModelDiffer(jsonSerializerOptions);
        return eventStore.ReadModels.Materialized.ObserveInstances<TReadModel>().Subscribe(window =>
        {
            foreach (var change in differ.Diff(window.Cast<object>()))
            {
                DispatchMaterialized(reactorType, typeof(TReadModel), change.Instance, change.ChangeType, change.ModelKey);
            }
        });
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
