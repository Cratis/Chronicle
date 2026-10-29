// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_all_tracked_users_are_locked_out : given.a_sign_in_throttle
{
    void Establish()
    {
        _settings = new() { MaxTrackedEntries = 10 };
        Create();
        for (var user = 0; user < 10; user++)
        {
            for (var attempt = 0; attempt < 5; attempt++) _throttle.Failed($"user-{user}", "192.0.2.1");
        }
    }

    void Because() => _throttle.Failed("new-user", "192.0.2.2");

    [Fact] void should_admit_the_new_user() => _throttle.IsLockedOut("new-user", "192.0.2.2").ShouldBeFalse();
    [Fact] void should_not_lock_out_an_untracked_user_on_the_same_address() => _throttle.IsLockedOut("other-user", "192.0.2.1").ShouldBeFalse();
    [Fact] void should_bound_both_counter_tables() => _throttle.TrackedEntries.ShouldEqual(20);
}
