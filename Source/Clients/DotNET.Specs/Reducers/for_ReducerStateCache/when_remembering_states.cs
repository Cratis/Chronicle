// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_ReducerStateCache;

public class when_remembering_states : Specification
{
    ReducerStateCache _cache;
    string? _first;
    string? _otherReducer;
    bool _forgottenFound;
    bool _allForgottenFound;
    bool _nullledFound;

    void Establish() => _cache = new(2);

    void Because()
    {
        _cache.Set("reducer", "a", "{\"v\":1}");
        _cache.Set("other", "a", "{\"v\":2}");
        _cache.TryGet("reducer", "a", out _first);
        _cache.TryGet("other", "a", out _otherReducer);

        _cache.Forget("reducer", "a");
        _forgottenFound = _cache.TryGet("reducer", "a", out _);

        _cache.Set("other", "b", "{}");
        _cache.ForgetAll("other");
        _allForgottenFound = _cache.TryGet("other", "a", out _) || _cache.TryGet("other", "b", out _);

        _cache.Set("reducer", "c", "{}");
        _cache.Set("reducer", "c", null);
        _nullledFound = _cache.TryGet("reducer", "c", out _);
    }

    [Fact] void should_hold_the_state_per_reducer_and_partition() => _first.ShouldEqual("{\"v\":1}");
    [Fact] void should_keep_reducers_apart() => _otherReducer.ShouldEqual("{\"v\":2}");
    [Fact] void should_forget_a_partition() => _forgottenFound.ShouldBeFalse();
    [Fact] void should_forget_every_partition_of_a_reducer() => _allForgottenFound.ShouldBeFalse();
    [Fact] void should_forget_a_partition_given_no_state() => _nullledFound.ShouldBeFalse();
}
