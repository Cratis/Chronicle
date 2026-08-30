// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Security.for_ChangeUserPassword.when_handling;

public class and_confirmation_does_not_match : given.a_change_user_password_command
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(async () =>
        await new ChangeUserPassword(UserIdentifier, OldPassword, NewPassword, "something-else").Handle(_grainFactory, _storage));

    [Fact] void should_throw_password_confirmation_mismatch() => _exception.ShouldBeOfExactType<PasswordConfirmationMismatch>();
    [Fact] void should_not_append_anything() =>
        _eventLog.DidNotReceive().Append(Arg.Any<EventSourceId>(), Arg.Any<object>());
}
