// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Security.for_ChangeUserPassword.when_handling;

/// <summary>
/// The System event store stays writable for the kernel's own user management, which does not go through the public append commands.
/// </summary>
public class and_the_password_is_changed : given.a_change_user_password_command
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(async () => await new ChangeUserPassword(UserIdentifier, OldPassword, NewPassword, NewPassword).Handle(_grainFactory, _storage));

    [Fact] void should_succeed() => _exception.ShouldBeNull();
    [Fact] void should_append_to_the_system_event_store() => _grainFactory.Received(1).GetGrain<IEventSequence>(Arg.Is<string>(key => key.Contains("System", StringComparison.OrdinalIgnoreCase)));
    [Fact] void should_append_the_password_change() => _eventLog.Received(1).Append(Arg.Any<EventSourceId>(), Arg.Any<UserPasswordChanged>());
}
