// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A fake factory of the connection of a dialer link: it hands out the same producer and acceptor, and counts connections opened and closed.
/// </summary>
/// <param name="pull">The fake producer.</param>
/// <param name="serve">The fake acceptor.</param>
public class FakeDialClients(FakePull pull, FakeServe serve) : IFederationDialClientFactory
{
    public string? Serial { get; set; } = "serial-1";

    public int Created { get; private set; }

    public int Closed { get; private set; }

    public Task<string?> GetCurrentSerial() => Task.FromResult(Serial);

    public Task<IFederationPullConnection> Create(FederationBindingState binding)
    {
        Created++;
        return Task.FromResult<IFederationPullConnection>(new Connection(this, pull, serve));
    }

    sealed class Connection(FakeDialClients owner, IFederationPull service, IFederationServe serveService) : IFederationPullConnection
    {
        public IFederationPull Service { get; } = service;

        public IFederationServe ServeService { get; } = serveService;

        public void Dispose() => owner.Closed++;
    }
}
