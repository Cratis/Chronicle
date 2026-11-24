// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Supervision;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clients.for_ConnectedClients;

public class when_deactivating : Specification
{
    ConnectedClients _connectedClients = default!;
    ISiloSupervisor _supervisor = default!;

    void Establish()
    {
        _supervisor = Substitute.For<ISiloSupervisor>();
        _connectedClients = new ConnectedClients(
            Substitute.For<ILogger<ConnectedClients>>(),
            Substitute.For<IMeter<ConnectedClients>>(),
            Options.Create(new ChronicleOptions()),
            Substitute.For<IStorage>(),
            Substitute.For<IClientDepartureNotifier>(),
            _supervisor);
    }

    async Task Because() => await _connectedClients.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.None, string.Empty), CancellationToken.None);

    [Fact] void should_unregister_from_the_supervisor() => _supervisor.Received(1).Unregister(Arg.Any<string>());
}
