// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Server.Authentication.for_ChronicleSignInManager;

public class when_the_password_is_correct : given.a_password_sign_in
{
    void Establish() => _hasher.VerifyHashedPassword(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>()).Returns(PasswordVerificationResult.Success);
    Task Because() => SignIn();
    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_reset_the_failures() => _throttle.Received(1).Succeeded("admin", string.Empty);
    [Fact] void should_not_record_a_failure() => _throttle.DidNotReceive().Failed(Arg.Any<string>(), Arg.Any<string>());
}
