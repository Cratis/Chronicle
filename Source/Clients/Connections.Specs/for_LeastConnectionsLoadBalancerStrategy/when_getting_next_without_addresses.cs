// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LeastConnectionsLoadBalancerStrategy;

public class when_getting_next_without_addresses : Specification
{
    LeastConnectionsLoadBalancerStrategy _strategy;
    Exception _exception;

    void Establish() => _strategy = new LeastConnectionsLoadBalancerStrategy(skipTlsValidation: true);

    void Destroy() => _strategy.Dispose();

    async Task Because() => _exception = await Catch.Exception(() => _strategy.Next([]));

    [Fact] void should_throw_missing_server_address() => _exception.ShouldBeOfExactType<MissingServerAddress>();
}
