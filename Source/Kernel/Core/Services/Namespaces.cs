// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Represents an implementation of <see cref="INamespaces"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage">The <see cref="IStorage"/> for working with the storage.</param>
internal sealed class Namespaces(IGrainFactory grainFactory, IStorage storage) : INamespaces
{
    /// <inheritdoc/>
    public async Task Ensure(EnsureNamespace command)
    {
        var namespaces = grainFactory.GetGrain<Chronicle.Namespaces.INamespaces>(command.EventStore);
        await namespaces.Ensure(command.Name);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetNamespaces(GetNamespacesRequest request)
    {
        var eventStore = storage.GetEventStore(request.EventStore);
        var namespaces = await eventStore.Namespaces.GetAll();
        return namespaces.Select(_ => _.Name.Value).ToArray();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<string>> ObserveNamespaces(GetNamespacesRequest request, CallContext context = default)
    {
        var eventStore = storage.GetEventStore(request.EventStore);
        return eventStore.Namespaces
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .Select(_ => _.Select(_ => _.Name.Value).ToArray());
    }
}
