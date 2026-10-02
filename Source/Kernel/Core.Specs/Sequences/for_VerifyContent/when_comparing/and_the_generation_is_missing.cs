// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_generation_is_missing : given.a_stored_event
{
    void Establish() => _command = _command with { EventType = new("event", 3, false) };

    async Task Because() => _result = await _command.Handle(_storage, _manager);

    [Fact] void should_not_invent_a_migration() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
