// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_usernames_differ_only_by_case : given.a_sign_in_throttle
{
    void Establish()
    {
        Create();
        Fail(4);
    }
    void Because() => Fail(1, "ADMIN");
    [Fact] void should_share_the_same_key() => _throttle.IsLockedOut("Admin", "192.0.2.1").ShouldBeTrue();
}
