// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Orleans.Storage;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling namespace state.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
public class NamespacesStateStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<NamespacesState>)!;
        var eventStoreName = grainId.Key.ToString()!;
        var eventStore = storage.GetEventStore(eventStoreName);

        var namespaces = (await eventStore.Namespaces.GetAll()).Select(_ => new NamespaceState(_.Name, _.Created)).ToList();
        if (namespaces.Find(_ => _.Name == EventStoreNamespaceName.Default) == default)
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

        foreach (var @namespace in actualGrainState.State.NewNamespaces)
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
