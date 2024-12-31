// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Represents an implementation of <see cref="INamespaces"/>.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> for working with the storage.</param>
public class Namespaces(IStorage storage) : INamespaces
{
    /// <inheritdoc/>
    public Task Ensure(EnsureNamespace command) =>
        storage.GetEventStore(command.EventStore).Namespaces.Ensure(command.Name);

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetNamespaces(GetNamespacesRequest request)
    {
        var eventStore = storage.GetEventStore(request.EventStore);
        var namespaces = await eventStore.Namespaces.GetAll();
        return namespaces.Select(_ => _.Name.Value).ToArray();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<string>> ObserveNamespaces(GetNamespacesRequest request)
    {
        var eventStore = storage.GetEventStore(request.EventStore);
        return eventStore.Namespaces.ObserveAll().Select(_ => _.Select(_ => _.Name.Value).ToArray());
    }
}
