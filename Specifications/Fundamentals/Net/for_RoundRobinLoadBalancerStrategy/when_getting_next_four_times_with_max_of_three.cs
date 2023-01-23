// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Net.for_RoundRobinLoadBalancerStrategy;

public class when_getting_next_four_times_with_max_of_three : Specification
{
    RoundRobinLoadBalancerStrategy strategy;
    int first;
    int second;
    int third;
    int forth;

    void Establish() => strategy = new();

    void Because()
    {
        first = strategy.GetNext(3);
        second = strategy.GetNext(3);
        third = strategy.GetNext(3);
        forth = strategy.GetNext(3);
    }

    [Fact] void first_should_be_0() => first.ShouldEqual(0);
    [Fact] void second_should_be_1() => second.ShouldEqual(1);
    [Fact] void third_should_be_2() => third.ShouldEqual(2);
    [Fact] void forth_should_be_0() => forth.ShouldEqual(0);
}
