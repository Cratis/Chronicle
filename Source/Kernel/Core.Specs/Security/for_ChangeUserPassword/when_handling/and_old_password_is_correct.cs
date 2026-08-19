// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Security.for_ChangeUserPassword.when_handling;

public class and_old_password_is_correct : given.a_change_user_password_command
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(async () =>
        await new ChangeUserPassword(UserIdentifier, OldPassword, NewPassword, NewPassword).Handle(_grainFactory, _storage));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_append_password_changed_for_the_user() =>
        _eventLog.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == UserIdentifier.ToString()),
            Arg.Is<UserPasswordChanged>(@event =>
                new PasswordHasher<object>().VerifyHashedPassword(null!, @event.PasswordHash, NewPassword) == PasswordVerificationResult.Success));
}
