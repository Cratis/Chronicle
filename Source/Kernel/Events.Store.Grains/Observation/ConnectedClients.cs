// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Execution;
using Microsoft.AspNetCore.Connections;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IConnectedClients"/>.
    /// </summary>
    [Singleton]
    public class ConnectedClients : IConnectedClients
    {
        readonly ConcurrentBag<ConnectionContext> _connectedClients = new();

        /// <inheritdoc/>
        public int Count => _connectedClients.Count;

        /// <inheritdoc/>
        public bool AnyConnectedClients => !_connectedClients.IsEmpty;

        /// <inheritdoc/>
        public event ClientDisconnected ClientDisconnected = (_) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task OnClientConnected(ConnectionContext context)
        {
            _connectedClients.Add(context);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task OnClientDisconnected(ConnectionContext context)
        {
            var remaining = _connectedClients.Except(new[] { context }).ToArray();
            _connectedClients.Clear();
            foreach (var remainingClient in remaining)
            {
                _connectedClients.Add(remainingClient);
            }
            await ClientDisconnected(context);
        }
    }
}
