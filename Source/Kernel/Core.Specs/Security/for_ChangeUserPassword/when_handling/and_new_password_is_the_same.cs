// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Security.for_ChangeUserPassword.when_handling;

public class and_new_password_is_the_same : given.a_change_user_password_command
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(async () =>
        await new ChangeUserPassword(UserIdentifier, OldPassword, OldPassword, OldPassword).Handle(_grainFactory, _storage));

    [Fact] void should_throw_new_password_must_be_different() => _exception.ShouldBeOfExactType<NewPasswordMustBeDifferent>();
    [Fact] void should_not_append_anything() =>
        _eventLog.DidNotReceive().Append(Arg.Any<EventSourceId>(), Arg.Any<object>());
}
