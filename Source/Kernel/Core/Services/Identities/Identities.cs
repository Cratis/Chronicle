// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Identities;

/// <summary>
/// Represents an implementation of <see cref="IIdentities"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> instance.</param>
internal sealed class Identities(IStorage storage) : IIdentities
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Identity>> GetIdentities(GetIdentitiesRequest request, CallContext context = default)
    {
        var identities = await storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).Identities.GetAll();
        return identities.ToContract();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<Identity>> ObserveIdentities(GetIdentitiesRequest request, CallContext context = default) =>
        storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace).Identities
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .Select(_ => _.ToContract());
}
