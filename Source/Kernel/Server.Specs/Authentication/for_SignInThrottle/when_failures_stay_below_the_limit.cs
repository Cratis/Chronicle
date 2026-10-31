// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_failures_stay_below_the_limit : given.a_sign_in_throttle
{
    void Establish() => Create();
    void Because() => Fail(4);
    [Fact] void should_allow_another_password_check() => _throttle.IsLockedOut("admin", "192.0.2.1").ShouldBeFalse();
}
