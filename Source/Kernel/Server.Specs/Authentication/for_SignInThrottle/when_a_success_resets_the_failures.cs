// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_a_success_resets_the_failures : given.a_sign_in_throttle
{
    void Establish()
    {
        Create();
        Fail(4);
    }

    void Because()
    {
        _throttle.Succeeded("admin", "192.0.2.1");
        Fail(1);
    }
    [Fact] void should_start_a_new_window() => _throttle.IsLockedOut("admin", "192.0.2.1").ShouldBeFalse();
}
