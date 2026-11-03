// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_ChronicleSignInManager;

public class when_the_password_is_incorrect : given.a_password_sign_in
{
    Task Because() => SignIn();
    [Fact] void should_fail() => _result.Succeeded.ShouldBeFalse();
    [Fact] void should_record_the_failure() => _throttle.Received(1).Failed("admin", string.Empty);
    [Fact] void should_not_reset_the_failures() => _throttle.DidNotReceive().Succeeded(Arg.Any<string>(), Arg.Any<string>());
}
