// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LeastConnectionsLoadBalancerStrategy;

public class when_creating_owned_probe_connections : Specification
{
    LeastConnectionsLoadBalancerStrategy _strategy;
    SocketsHttpHandler _handler;

    void Establish() => _strategy = new LeastConnectionsLoadBalancerStrategy(skipTlsValidation: false);

    void Because() => _handler = (SocketsHttpHandler)_strategy.ProbeHandler!;

    void Destroy() => _strategy.Dispose();

    [Fact] void should_retire_pooled_connections_to_refresh_dns() => _handler.PooledConnectionLifetime.ShouldEqual(TimeSpan.FromSeconds(30));
}
