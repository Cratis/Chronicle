// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_RoundRobinLoadBalancerStrategy;

public class when_getting_next_without_addresses : Specification
{
    RoundRobinLoadBalancerStrategy _strategy;
    Exception _exception;

    void Establish() => _strategy = new RoundRobinLoadBalancerStrategy();

    void Because() => _exception = Catch.Exception(() => _strategy.Next([]));

    [Fact] void should_throw_missing_server_address() => _exception.ShouldBeOfExactType<MissingServerAddress>();
}
