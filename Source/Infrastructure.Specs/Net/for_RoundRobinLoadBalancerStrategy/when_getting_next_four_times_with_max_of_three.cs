// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Net.for_RoundRobinLoadBalancerStrategy;

public class when_getting_next_four_times_with_max_of_three : Specification
{
    RoundRobinLoadBalancerStrategy _strategy;
    int _first;
    int _second;
    int _third;
    int _forth;

    void Establish() => _strategy = new();

    void Because()
    {
        _first = _strategy.GetNext(3);
        _second = _strategy.GetNext(3);
        _third = _strategy.GetNext(3);
        _forth = _strategy.GetNext(3);
    }

    [Fact] void should_have_first_be_0() => _first.ShouldEqual(0);
    [Fact] void should_have_second_be_1() => _second.ShouldEqual(1);
    [Fact] void should_have_third_be_2() => _third.ShouldEqual(2);
    [Fact] void should_have_forth_be_0() => _forth.ShouldEqual(0);
}
