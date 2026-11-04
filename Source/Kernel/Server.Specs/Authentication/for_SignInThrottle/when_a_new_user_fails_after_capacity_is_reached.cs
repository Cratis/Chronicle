// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_a_new_user_fails_after_capacity_is_reached : given.a_sign_in_throttle
{
    void Establish()
    {
        _settings = new() { MaxTrackedEntries = 10 };
        Create();
        for (var user = 0; user < 10; user++) _throttle.Failed($"user-{user}", "192.0.2.1");
    }

    void Because()
    {
        for (var attempt = 0; attempt < 50; attempt++) _throttle.Failed("new-user", $"198.51.100.{attempt}");
    }

    [Fact] void should_track_and_lock_out_the_new_username() => _throttle.IsLockedOut("new-user", "2001:db8::1").ShouldBeTrue();
    [Fact] void should_keep_memory_bounded() => (_throttle.TrackedEntries <= 20).ShouldBeTrue();
}
