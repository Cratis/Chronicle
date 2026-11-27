// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Namespaces;

/// <summary>Routes opt-in operations without silently falling back to unfenced storage.</summary>
/// <param name="storage">The infrastructure storage.</param>
/// <param name="grains">The grain factory.</param>
/// <param name="options">The deployment configuration.</param>
public sealed class NamespaceIncarnations(IStorage storage, IGrainFactory grains, IOptions<ChronicleOptions> options) : INamespaceIncarnations
{
    /// <inheritdoc/>
    public bool Enabled => options.Value.Features.NamespaceIncarnations;

    /// <inheritdoc/>
    /// <exception cref="NamespaceIncarnationUnavailable">Coordination is disabled or the provider is not fenced.</exception>
    public IHaveNamespaceIncarnations GetStorage(EventStoreName eventStore)
    {
        if (!Enabled) throw new NamespaceIncarnationUnavailable(eventStore);
        return storage.GetEventStore(eventStore) as IHaveNamespaceIncarnations ?? throw new NamespaceIncarnationUnavailable(eventStore);
    }

    /// <inheritdoc/>
    public Task<EventStoreNamespaceName> Ensure(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        GetStorage(eventStore);
        return Owner(eventStore, @namespace).Ensure(@namespace);
    }

    /// <inheritdoc/>
    /// <exception cref="NamespaceCannotBeRemoved">The default namespace cannot be removed.</exception>
    /// <exception cref="NamespaceIncarnationUnavailable">An unknown namespace has no lifecycle to remove.</exception>
    public async Task Remove(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        if (@namespace.Value.Equals(EventStoreNamespaceName.Default.Value, StringComparison.OrdinalIgnoreCase)) throw new NamespaceCannotBeRemoved(eventStore, @namespace);
        var capability = GetStorage(eventStore);
        var current = await capability.NamespaceLifecycles.Get(@namespace);
        if (current is null)
        {
            var canonical = await GetCanonical(eventStore, @namespace) ?? throw new NamespaceIncarnationUnavailable(eventStore);
            await Ensure(eventStore, canonical);
        }
        await Owner(eventStore, @namespace).Remove();
    }

    /// <inheritdoc/>
    public async Task<EventStoreNamespaceName?> GetCanonical(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        GetStorage(eventStore);
        var names = await storage.GetEventStore(eventStore).Namespaces.GetAll();
        var found = names.FirstOrDefault(_ => _.Name.Value.Equals(@namespace.Value, StringComparison.OrdinalIgnoreCase))?.Name;
        return found?.Value.Equals(EventStoreNamespaceName.Default.Value, StringComparison.OrdinalIgnoreCase) == true ? EventStoreNamespaceName.Default : found;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreNamespaceName>> GetAll(EventStoreName eventStore)
    {
        var capability = GetStorage(eventStore);
        var result = new List<EventStoreNamespaceName>();
        foreach (var entry in await storage.GetEventStore(eventStore).Namespaces.GetAll())
        {
            var state = await capability.NamespaceLifecycles.Get(entry.Name);
            if (state is null || state.Status == NamespaceLifecycleStatus.Open) result.Add(entry.Name);
        }
        return result;
    }

    /// <inheritdoc/>
    /// <exception cref="NamespaceLifecycleChanged">The callback does not match an issued Forget checkpoint.</exception>
    public async Task ValidateForget(EventStoreName eventStore, EventStoreNamespaceName @namespace, NamespaceIncarnationId incarnation, Guid operation)
    {
        var state = await GetStorage(eventStore).NamespaceLifecycles.Get(@namespace);
        if (state is null || state.Incarnation != incarnation || state.OperationId != operation ||
            state.Status != NamespaceLifecycleStatus.Closing || state.Stage != NamespaceRemovalStage.Forget ||
            !state.AttemptInProgress || !state.DestructiveAttemptOutstanding)
        {
            throw new NamespaceLifecycleChanged(@namespace);
        }
    }

    /// <inheritdoc/>
    public Task Resume(EventStoreName eventStore, EventStoreNamespaceName @namespace, Guid operation, long revision)
    {
        GetStorage(eventStore);
        return Owner(eventStore, @namespace).Resume(operation, revision);
    }

    /// <inheritdoc/>
    public Task Abort(EventStoreName eventStore, EventStoreNamespaceName @namespace, Guid operation, long revision)
    {
        GetStorage(eventStore);
        return Owner(eventStore, @namespace).Abort(operation, revision);
    }

    INamespaceIncarnation Owner(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        grains.GetGrain<INamespaceIncarnation>(new NamespaceIncarnationKey(eventStore.Value, @namespace.Value).ToString());
}
