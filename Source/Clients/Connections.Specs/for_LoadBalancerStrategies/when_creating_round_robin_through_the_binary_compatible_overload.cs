// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LoadBalancerStrategies;

public class when_creating_round_robin_through_the_binary_compatible_overload : Specification
{
    ILoadBalancerStrategy _strategy;

    void Because() => _strategy = (ILoadBalancerStrategy)typeof(LoadBalancerStrategies)
        .GetMethod(nameof(LoadBalancerStrategies.Create), [typeof(string), typeof(bool)])!
        .Invoke(null, [RoundRobinLoadBalancerStrategy.StrategyName, false])!;

    [Fact] void should_create_round_robin() => _strategy.ShouldBeOfExactType<RoundRobinLoadBalancerStrategy>();
}
