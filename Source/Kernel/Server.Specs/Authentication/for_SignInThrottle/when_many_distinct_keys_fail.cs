// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_many_distinct_keys_fail : given.a_sign_in_throttle
{
    void Establish()
    {
        _settings = new() { MaxTrackedEntries = 10 };
        Create();
    }
    void Because() => Parallel.For(0, 1000, index => Fail(5, $"user-{index}"));
    [Fact] void should_bound_memory() => _throttle.TrackedEntries.ShouldEqual(10);
    [Fact] void should_defer_new_pairs_until_capacity_is_available() => _throttle.IsLockedOut("new-user", "192.0.2.1").ShouldBeTrue();
}
