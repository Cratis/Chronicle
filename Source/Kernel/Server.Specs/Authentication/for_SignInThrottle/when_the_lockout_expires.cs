// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_the_lockout_expires : given.a_sign_in_throttle
{
    void Establish()
    {
        Create();
        Fail(5);
    }
    void Because() => _clock.Advance(300);
    [Fact] void should_allow_another_password_check() => _throttle.IsLockedOut("admin", "192.0.2.1").ShouldBeFalse();
    [Fact] void should_remove_expired_entries()
    {
        _throttle.IsLockedOut("admin", "192.0.2.1");
        _throttle.TrackedEntries.ShouldEqual(0);
    }
}
