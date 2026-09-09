// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Security.for_ChangeUserPassword.when_handling;

public class and_appending_fails : given.a_change_user_password_command
{
    Exception _exception;

    void Establish() => _eventLog.Append(
        Arg.Any<EventSourceId>(),
        Arg.Any<object>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Causation>>(),
        Arg.Any<Identity>(),
        Arg.Any<IEnumerable<Tag>>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>()).Returns(AppendResult.Failed(CorrelationId.New(), [new AppendError("storage failed")]));

    async Task Because() => _exception = await Catch.Exception(() => new ChangeUserPassword(UserIdentifier, OldPassword, NewPassword, NewPassword).Handle(_grainFactory, _storage));

    [Fact] void should_report_failure() => _exception.ShouldBeOfExactType<PasswordCouldNotBeChanged>();
}
