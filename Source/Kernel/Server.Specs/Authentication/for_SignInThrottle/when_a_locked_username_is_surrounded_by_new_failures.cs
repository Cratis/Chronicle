// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_a_locked_username_is_surrounded_by_new_failures : given.a_sign_in_throttle
{
    void Establish()
    {
        _settings = new() { MaxTrackedEntries = 10 };
        Create();
        for (var address = 1; address <= 50; address++) _throttle.Failed("admin", $"192.0.2.{address}");
    }

    void Because()
    {
        for (var user = 0; user < 100; user++) _throttle.Failed($"user-{user}", "198.51.100.1");
    }

    [Fact] void should_retain_the_locked_username() => _throttle.IsLockedOut("admin", "198.51.100.2").ShouldBeTrue();
    [Fact] void should_allow_an_untracked_username() => _throttle.IsLockedOut("new-user", "192.0.2.1").ShouldBeFalse();
    [Fact] void should_bound_both_counter_tables() => _throttle.TrackedEntries.ShouldEqual(20);
}
