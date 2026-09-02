// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Security.for_ChangeUserPassword.when_handling;

/// <summary>
/// The current password is what makes this a change rather than a takeover, so it is verified against the stored
/// hash before anything is appended.
/// </summary>
public class and_old_password_is_wrong : given.a_change_user_password_command
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(async () =>
        await new ChangeUserPassword(UserIdentifier, "not-the-old-password", NewPassword, NewPassword).Handle(_grainFactory, _storage));

    [Fact] void should_throw_invalid_old_password() => _exception.ShouldBeOfExactType<InvalidOldPassword>();
    [Fact] void should_not_append_anything() =>
        _eventLog.DidNotReceive().Append(Arg.Any<EventSourceId>(), Arg.Any<object>());
}
