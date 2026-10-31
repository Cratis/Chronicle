// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_the_username_ceiling_is_configured : given.a_sign_in_throttle
{
    bool _lockedBeforeCeiling;

    void Establish()
    {
        _settings = new() { MaxFailedAttemptsPerUsername = 12 };
        Create();
        for (var address = 1; address < 12; address++) _throttle.Failed("admin", $"192.0.2.{address}");
        _lockedBeforeCeiling = _throttle.IsLockedOut("admin", "198.51.100.1");
    }

    void Because() => _throttle.Failed("admin", "192.0.2.12");

    [Fact] void should_allow_sign_in_below_the_configured_ceiling() => _lockedBeforeCeiling.ShouldBeFalse();
    [Fact] void should_lock_out_at_the_configured_ceiling() => _throttle.IsLockedOut("admin", "198.51.100.1").ShouldBeTrue();
}
