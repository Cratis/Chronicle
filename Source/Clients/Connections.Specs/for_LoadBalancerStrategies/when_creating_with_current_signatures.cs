// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LoadBalancerStrategies;

public class when_creating_with_current_signatures : Specification
{
    ILoadBalancerStrategy[] _strategies;

    void Because() => _strategies =
    [
        LoadBalancerStrategies.Create(),
        LoadBalancerStrategies.Create(LeastConnectionsLoadBalancerStrategy.StrategyName),
        LoadBalancerStrategies.Create(LeastConnectionsLoadBalancerStrategy.StrategyName, true),
        LoadBalancerStrategies.Create(disableTls: true)
    ];

    void Destroy()
    {
        foreach (var strategy in _strategies.OfType<IDisposable>())
        {
            strategy.Dispose();
        }
    }

    [Fact] void should_resolve_the_existing_call_forms() => _strategies.ShouldEachConformTo(_ => _ is LeastConnectionsLoadBalancerStrategy);
    [Fact] void should_keep_the_current_optional_defaults() => typeof(LoadBalancerStrategies).GetMethod(nameof(LoadBalancerStrategies.Create), [typeof(string), typeof(bool), typeof(bool)])!.GetParameters().Select(_ => _.DefaultValue).ShouldEqual([null, false, false]);
}
