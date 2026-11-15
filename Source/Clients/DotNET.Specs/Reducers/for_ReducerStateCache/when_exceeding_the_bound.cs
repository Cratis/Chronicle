// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerStateCache;

public class when_exceeding_the_bound : Specification
{
    ReducerStateCache _cache;

    void Establish() => _cache = new(2);

    void Because()
    {
        _cache.Set("reducer", "a", "{}");
        _cache.Set("reducer", "b", "{}");
        _cache.Set("reducer", "c", "{}");
    }

    [Fact] void should_not_hold_more_than_the_bound() => _cache.Count.ShouldEqual(2);
    [Fact] void should_hold_the_newest() => _cache.TryGet("reducer", "c", out _).ShouldBeTrue();
}
