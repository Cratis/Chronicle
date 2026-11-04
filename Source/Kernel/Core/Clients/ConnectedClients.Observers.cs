// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Clients;

public partial class ConnectedClients
{
    /// <inheritdoc/>
    public Task RecordObserver(ConnectionId connectionId, ObserverKey observer)
    {
        if (_clients.TryGetValue(connectionId, out var entry))
        {
            entry.Observers.Add(observer);
        }

        return Task.CompletedTask;
    }
}
