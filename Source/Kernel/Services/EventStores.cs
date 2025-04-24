// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Reactive;
using Cratis.Chronicle.Storage;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services;

/// <summary>.
/// Represents an implementation of <see cref="IEventStores"/>.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> for working with the storage.</param>
internal class EventStores(IStorage storage) : IEventStores
{
    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetEventStores()
    {
        var eventStores = await storage.GetEventStores();
        return eventStores.Select(_ => _.Value).ToArray();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<string>> ObserveEventStores(CallContext callContext = default) =>
        storage
            .ObserveEventStores()
            .CompletedBy(callContext.CancellationToken)
            .Select(_ => _.Select(e => e.Value));
}
