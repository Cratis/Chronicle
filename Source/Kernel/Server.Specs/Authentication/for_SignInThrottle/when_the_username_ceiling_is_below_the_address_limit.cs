// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_the_username_ceiling_is_below_the_address_limit : given.a_sign_in_throttle
{
    void Establish()
    {
        _settings = new() { MaxFailedAttemptsPerUsername = 1 };
        Create();
    }

    void Because() => Fail(100);

    [Fact] void should_lock_out_the_failing_address() => _throttle.IsLockedOut("admin", "192.0.2.1").ShouldBeTrue();
    [Fact] void should_not_lock_out_other_addresses() => _throttle.IsLockedOut("admin", "198.51.100.1").ShouldBeFalse();
}
