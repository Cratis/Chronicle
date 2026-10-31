// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_throttling_is_disabled : given.a_sign_in_throttle
{
    void Establish()
    {
        _settings = new() { MaxFailedAttempts = 0 };
        Create();
    }
    void Because() => Fail(100);
    [Fact] void should_allow_password_checks() => _throttle.IsLockedOut("admin", "192.0.2.1").ShouldBeFalse();
    [Fact] void should_not_track_entries() => _throttle.TrackedEntries.ShouldEqual(0);
}
