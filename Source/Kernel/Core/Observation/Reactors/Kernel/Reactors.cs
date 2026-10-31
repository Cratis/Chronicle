// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Cratis.Types;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.Observation.Reactors.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IReactors"/>.
/// </summary>
/// <param name="types">The <see cref="ITypes"/> to use for discovering reactors.</param>
/// <param name="localSiloDetails">The local silo details.</param>
/// <param name="storage">The <see cref="IStorage"/> to use for working with underlying storage.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> to use for creating reactor grains.</param>
[Singleton]
public class Reactors(
    ITypes types,
    ILocalSiloDetails localSiloDetails,
    IStorage storage,
    IGrainFactory grainFactory) : IReactors, IAsyncDisposable
{
    readonly IEnumerable<Type> _reactorTypes = types.FindMultiple<IReactor>().Where(t => t != typeof(Reactor)).ToArray();
    readonly SemaphoreSlim _definitionWrites = new(1, 1);
    readonly CancellationTokenSource _shutdown = new();
    readonly object _disposal = new();
    Task? _disposeTask;

    /// <inheritdoc/>
    public Task DiscoverAndRegister(EventStoreName eventStore, EventStoreNamespaceName namespaceName) =>
        DiscoverAndRegister(eventStore, namespaceName, CancellationToken.None);

    /// <inheritdoc/>
    public async Task DiscoverAndRegister(EventStoreName eventStore, EventStoreNamespaceName namespaceName, CancellationToken cancellationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
        cancellationToken = cancellation.Token;
        var subscribeMethod = GetType().GetMethod(nameof(Subscribe), BindingFlags.Instance | BindingFlags.NonPublic)!;
        foreach (var reactor in _reactorTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await (subscribeMethod
                .MakeGenericMethod(reactor)
                .Invoke(this, [eventStore, namespaceName, cancellationToken])! as Task)!;
        }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        lock (_disposal)
        {
            return new ValueTask(_disposeTask ??= StopDefinitionWrites());
        }
    }

    async Task StopDefinitionWrites()
    {
        // Cancel queued admissions, then let the admitted storage operation release its lease before
        // disposing the semaphore. Disposing while a write is in flight would mask its result on Release.
        await _shutdown.CancelAsync();
        await _definitionWrites.WaitAsync();
        _definitionWrites.Dispose();
        _shutdown.Dispose();
        GC.SuppressFinalize(this);
    }

    async Task Subscribe<TReactor>(EventStoreName eventStore, EventStoreNamespaceName namespaceName, CancellationToken cancellationToken)
        where TReactor : IReactor
    {
        var system = typeof(TReactor).IsSystemEventStoreOnly();
        if (system && eventStore != EventStoreName.System)
        {
            return;
        }

        var defaultNamespaceOnly = typeof(TReactor).IsDefaultNamespaceOnly();
        if (defaultNamespaceOnly && namespaceName != EventStoreNamespaceName.Default)
        {
            return;
        }

        var reactorId = typeof(TReactor).GetReactorId();
        var reactorType = typeof(TReactor);
        var key = new ObserverKey(
            $"$system.{reactorId}",
            eventStore,
            namespaceName,
            reactorType.GetEventSequenceId());

        var reactorDefinition = new ReactorDefinition(
            key.ObserverId,
            ReactorOwner.Kernel,
            reactorType.GetEventSequenceId(),
            reactorType.GetEventTypes().Select(et => new EventTypeWithKeyExpression(et, WellKnownExpressions.EventSourceId)).ToArray(),
            false);

        // Definitions belong to the event store, not the namespace. Concurrent tenant startup must
        // not rewrite identical metadata for every tenant. Keep the read/check/write bounded per silo;
        // do not cache success across stores, activations or failed writes.
        await _definitionWrites.WaitAsync(cancellationToken);
        try
        {
            var definitions = storage.GetEventStore(eventStore).Reactors;
            var existing = await definitions.Has(reactorDefinition.Identifier)
                ? await definitions.Get(reactorDefinition.Identifier)
                : null;
            if (existing is null ||
                existing.Owner != reactorDefinition.Owner ||
                existing.EventSequenceId != reactorDefinition.EventSequenceId ||
                existing.IsReplayable != reactorDefinition.IsReplayable ||
                !existing.EventTypes.SequenceEqual(reactorDefinition.EventTypes) ||
                (existing.Tags?.Any() ?? false) ||
                (existing.Filters is { } filters &&
                    (filters.Tags.Any() ||
                     (filters.EventSourceType is not null && filters.EventSourceType != EventSourceType.Unspecified) ||
                     filters.EventStreamType is { IsAll: false })))
            {
                await definitions.Save(reactorDefinition);
            }
        }
        finally
        {
            _definitionWrites.Release();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var observer = grainFactory.GetGrain<IObserver>(key);
        await observer.Subscribe<IReactorObserverSubscriber<TReactor>>(
            ObserverType.Reactor,
            reactorType.GetEventTypes(),
            localSiloDetails.SiloAddress,
            null,
            false);
    }
}
