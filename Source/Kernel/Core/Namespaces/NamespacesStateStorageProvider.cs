// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Extensions.Logging;
using Orleans.Storage;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling namespace state.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
/// <param name="logger">Logger for namespace list repairs.</param>
public class NamespacesStateStorageProvider(IStorage storage, ILogger<NamespacesStateStorageProvider> logger) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<NamespacesState>)!;
        var eventStoreName = grainId.Key.ToString()!;
        var eventStore = storage.GetEventStore(eventStoreName);

        var stored = (await eventStore.Namespaces.GetAll()).ToList();
        var withOwnData = new HashSet<EventStoreNamespaceName>();
        foreach (var group in stored.GroupBy(_ => _.Name.Value, StringComparer.OrdinalIgnoreCase).Where(_ => _.Count() > 1))
        {
            foreach (var member in group)
            {
                if (await eventStore.HasOwnNamespaceData(member.Name)) withOwnData.Add(member.Name);
            }
        }
        var (namespaces, merged) = NamespaceCasingMerge.Merge(stored, withOwnData);
        foreach (var (variant, canonical) in merged)
        {
            logger.MergedNamespace(eventStoreName, variant, canonical);
            if (withOwnData.Contains(variant) && withOwnData.Count(_ => _.Value.Equals(canonical.Value, StringComparison.OrdinalIgnoreCase)) > 1)
            {
                logger.VariantDataLeftInPlace(eventStoreName, variant, canonical);
            }
            try
            {
                await eventStore.Namespaces.Delete(variant);
            }
            catch (Exception exception)
            {
                logger.CouldNotDeleteVariant(exception, eventStoreName, variant);
            }
        }
        if (!namespaces.Any(_ => _.Name.Value.Equals(EventStoreNamespaceName.Default.Value, StringComparison.OrdinalIgnoreCase)))
        {
            var @namespace = new NamespaceState(EventStoreNamespaceName.Default, DateTimeOffset.UtcNow);
            namespaces.Add(@namespace);
            await eventStore.Namespaces.Create(@namespace.Name, @namespace.Created);
        }

        actualGrainState.State = new NamespacesState
        {
            Namespaces = namespaces
        };
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<NamespacesState>)!;
        var eventStoreName = grainId.Key.ToString()!;
        var eventStore = storage.GetEventStore(eventStoreName);

        foreach (var @namespace in actualGrainState.State!.NewNamespaces)
        {
            await eventStore.Namespaces.Create(@namespace.Name, @namespace.Created);
        }

        foreach (var @namespace in actualGrainState.State.NewNamespaces)
        {
            actualGrainState.State.Namespaces.Add(@namespace);
        }
        actualGrainState.State.NewNamespaces.Clear();
    }
}
