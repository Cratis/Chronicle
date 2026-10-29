// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle;

public class when_failures_come_from_another_address : given.a_sign_in_throttle
{
    void Establish() => Create();
    void Because() => Fail(5);
    [Fact] void should_not_lock_out_another_address() => _throttle.IsLockedOut("admin", "192.0.2.2").ShouldBeFalse();
    [Fact] void should_not_lock_out_another_user() => _throttle.IsLockedOut("other", "192.0.2.1").ShouldBeFalse();
}
