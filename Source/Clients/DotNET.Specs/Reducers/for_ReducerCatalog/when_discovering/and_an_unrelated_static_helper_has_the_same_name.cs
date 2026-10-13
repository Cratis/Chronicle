// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerCatalog.when_discovering;

public class and_an_unrelated_static_helper_has_the_same_name : Specification
{
    bool _candidate;
    bool _self;

    void Because()
    {
        _candidate = SelfReducerMethods.IsDiscoveryCandidate(typeof(OrdinaryHelper));
        _self = SelfReducerMethods.HasSelfShape(typeof(OrdinaryHelper));
    }

    [Fact] void should_not_reserve_the_name_reduce() => _candidate.ShouldBeFalse();
    [Fact] void should_not_discover_unrelated_helpers() => _self.ShouldBeFalse();

    public class OrdinaryHelper(int value)
    {
        public int Value { get; } = value;
        public static int Reduce(int value, int current) => value + current;
    }
}
