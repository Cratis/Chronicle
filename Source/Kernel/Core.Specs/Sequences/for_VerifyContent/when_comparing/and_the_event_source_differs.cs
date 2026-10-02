// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_event_source_differs : given.a_stored_event
{
    void Establish() => _command = _command with { EventSourceId = "another-source" };

    async Task Because() => _result = await _command.Handle(_storage, _manager, _converter);

    [Fact] void should_not_compare_an_event_outside_the_requested_source() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
