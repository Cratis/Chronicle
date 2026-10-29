// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_addresses_rotate_between_failures : given.a_sign_in_throttle
{
    bool _lockedBeforeCeiling;

    void Establish()
    {
        Create();
        for (var attempt = 1; attempt < 50; attempt++) _throttle.Failed("ADMIN", $"192.0.2.{attempt}");
        _lockedBeforeCeiling = _throttle.IsLockedOut("admin", "198.51.100.1");
    }

    void Because() => _throttle.Failed("ADMIN", "192.0.2.50");

    [Fact] void should_allow_sign_in_below_the_default_ceiling() => _lockedBeforeCeiling.ShouldBeFalse();

    [Fact] void should_lock_out_the_username_on_a_new_ipv4_address() => _throttle.IsLockedOut("admin", "198.51.100.1").ShouldBeTrue();
    [Fact] void should_lock_out_the_username_on_a_new_ipv6_address() => _throttle.IsLockedOut("admin", "2001:db8:1::1").ShouldBeTrue();
    [Fact] void should_allow_another_username() => _throttle.IsLockedOut("other", "192.0.2.1").ShouldBeFalse();
}
