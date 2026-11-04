// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_a_locked_address_keeps_failing : given.a_sign_in_throttle
{
    void Establish()
    {
        Create();
        Fail(5);
    }

    void Because() => Fail(100);

    [Fact] void should_keep_the_address_locked_out() => _throttle.IsLockedOut("admin", "192.0.2.1").ShouldBeTrue();
    [Fact] void should_allow_the_username_from_another_address() => _throttle.IsLockedOut("admin", "198.51.100.1").ShouldBeFalse();
}
