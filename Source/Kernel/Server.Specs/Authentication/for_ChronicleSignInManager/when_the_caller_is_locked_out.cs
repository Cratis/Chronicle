// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Server.Authentication.for_ChronicleSignInManager;

public class when_the_caller_is_locked_out : given.a_password_sign_in
{
    void Establish() => _throttle.IsLockedOut(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
    Task Because() => SignIn();
    [Fact] void should_return_locked_out() => _result.IsLockedOut.ShouldBeTrue();
    [Fact] void should_not_check_the_password() => _hasher.DidNotReceive().VerifyHashedPassword(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>());
}
