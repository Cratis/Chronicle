// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LoadBalancerStrategies;

public class when_creating_without_name : Specification
{
    ILoadBalancerStrategy _strategy;

    void Because() => _strategy = LoadBalancerStrategies.Create();

    void Destroy() => (_strategy as IDisposable)?.Dispose();

    [Fact] void should_default_to_least_connections() => _strategy.ShouldBeOfExactType<LeastConnectionsLoadBalancerStrategy>();
}
