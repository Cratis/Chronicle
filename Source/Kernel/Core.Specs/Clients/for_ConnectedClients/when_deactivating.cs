// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_deactivating : Specification
{
    ConnectedClients _connectedClients = default!;
    IGrainTimer _timer = default!;

    void Establish()
    {
        _connectedClients = new ConnectedClients(
            Substitute.For<ILogger<ConnectedClients>>(),
            Substitute.For<IMeter<ConnectedClients>>());
        _timer = Substitute.For<IGrainTimer>();

        typeof(ConnectedClients)
            .GetField("_reviseConnectedClientsTimer", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_connectedClients, _timer);
    }

    async Task Because() => await _connectedClients.OnDeactivateAsync(default, default);

    [Fact] void should_dispose_timer() => _timer.Received(1).Dispose();
}
