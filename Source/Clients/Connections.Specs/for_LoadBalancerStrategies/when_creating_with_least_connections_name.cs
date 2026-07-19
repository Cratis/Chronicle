// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LoadBalancerStrategies;

public class when_creating_with_least_connections_name : Specification
{
    ILoadBalancerStrategy _strategy;

    void Because() => _strategy = LoadBalancerStrategies.Create(LeastConnectionsLoadBalancerStrategy.StrategyName);

    void Destroy() => (_strategy as IDisposable)?.Dispose();

    [Fact] void should_create_least_connections_strategy() => _strategy.ShouldBeOfExactType<LeastConnectionsLoadBalancerStrategy>();
}
