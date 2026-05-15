// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Namespaces;
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
    public Task EnsureNamespace(EnsureNamespaceRequest request, CallContext callContext = default) =>
        new Chronicle.Namespaces.EnsureNamespace(request.EventStore, request.Namespace)
            .Handle(grainFactory);

    /// <inheritdoc/>
    public IObservable<IEnumerable<string>> AllNamespaces(AllNamespacesRequest request, CallContext callContext = default) =>
        Chronicle.Namespaces.NamespaceNames.AllNamespaces(request.EventStore, storage)
            .CompletedBy(callContext.CancellationToken);
}
