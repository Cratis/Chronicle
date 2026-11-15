// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Connections.for_LoadBalancerStrategies;

public class when_creating_through_the_binary_compatible_overload : Specification
{
    MethodInfo _overload;
    ILoadBalancerStrategy _strategy;

    void Establish() => _overload = typeof(LoadBalancerStrategies).GetMethod(nameof(LoadBalancerStrategies.Create), [typeof(string), typeof(bool)])!;

    void Because()
    {
        _overload.ShouldNotBeNull();
        _strategy = (ILoadBalancerStrategy)_overload.Invoke(null, [null, false])!;
    }

    void Destroy() => (_strategy as IDisposable)?.Dispose();

    [Fact] void should_expose_the_exact_public_static_overload() => _overload.IsStatic.ShouldBeTrue();
    [Fact] void should_return_the_strategy_contract() => _overload.ReturnType.ShouldEqual(typeof(ILoadBalancerStrategy));
    [Fact] void should_require_both_parameters() => _overload.GetParameters().ShouldEachConformTo(_ => !_.IsOptional);
    [Fact] void should_default_to_least_connections() => _strategy.ShouldBeOfExactType<LeastConnectionsLoadBalancerStrategy>();
}
