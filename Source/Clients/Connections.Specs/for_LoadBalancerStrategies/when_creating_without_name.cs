// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LoadBalancerStrategies;

public class when_creating_without_name : Specification
{
    ILoadBalancerStrategy _strategy;

    void Because() => _strategy = LoadBalancerStrategies.Create();

    [Fact] void should_default_to_round_robin() => _strategy.ShouldBeOfExactType<RoundRobinLoadBalancerStrategy>();
}
