// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LoadBalancerStrategies;

public class when_creating_with_unknown_name : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => LoadBalancerStrategies.Create("unknown"));

    [Fact] void should_throw_unknown_load_balancer_strategy() => _exception.ShouldBeOfExactType<UnknownLoadBalancerStrategy>();
}
