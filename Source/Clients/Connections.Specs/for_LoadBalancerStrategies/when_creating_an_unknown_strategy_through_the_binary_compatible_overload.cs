// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Connections.for_LoadBalancerStrategies;

public class when_creating_an_unknown_strategy_through_the_binary_compatible_overload : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => typeof(LoadBalancerStrategies)
        .GetMethod(nameof(LoadBalancerStrategies.Create), [typeof(string), typeof(bool)])!
        .Invoke(null, ["unknown", false]));

    [Fact] void should_forward_the_unknown_strategy_error() => _exception.InnerException.ShouldBeOfExactType<UnknownLoadBalancerStrategy>();
    [Fact] void should_wrap_the_error_from_the_invoked_overload() => _exception.ShouldBeOfExactType<TargetInvocationException>();
}
