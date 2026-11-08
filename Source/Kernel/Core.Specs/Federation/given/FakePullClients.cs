// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A fake connection factory that always hands out the same producer and counts connections opened and closed.
/// </summary>
/// <param name="pull">The fake producer.</param>
public class FakePullClients(FakePull pull) : IFederationPullClientFactory
{
    public int Created { get; private set; }

    public int Closed { get; private set; }

    public Exception? FailWith { get; set; }

    public IFederationPullConnection Create(FederationBindingState binding)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        Created++;
        return new Connection(this, pull);
    }

    sealed class Connection(FakePullClients owner, IFederationPull service) : IFederationPullConnection
    {
        public IFederationPull Service { get; } = service;

        public void Dispose() => owner.Closed++;
    }
}
