// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LoadBalancerStrategies;

public class when_creating_with_random_name : Specification
{
    ILoadBalancerStrategy _strategy;

    void Because() => _strategy = LoadBalancerStrategies.Create(RandomLoadBalancerStrategy.StrategyName);

    [Fact] void should_create_random_strategy() => _strategy.ShouldBeOfExactType<RandomLoadBalancerStrategy>();
}
