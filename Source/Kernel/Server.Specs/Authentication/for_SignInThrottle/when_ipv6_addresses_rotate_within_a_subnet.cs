// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_ipv6_addresses_rotate_within_a_subnet : given.a_sign_in_throttle
{
    void Establish() => Create();

    void Because()
    {
        for (var attempt = 1; attempt <= 5; attempt++) _throttle.Failed("admin", $"2001:db8:1:2::{attempt}");
    }

    [Fact] void should_track_one_username_and_one_subnet() => _throttle.TrackedEntries.ShouldEqual(2);
    [Fact] void should_lock_out_a_new_address_in_the_subnet() => _throttle.IsLockedOut("admin", "2001:0db8:0001:0002::ffff").ShouldBeTrue();
}
