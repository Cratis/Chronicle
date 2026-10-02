// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_schema_is_missing : given.a_stored_event
{
    void Establish() => _storage.GetEventStore("store").EventTypes.HasFor("event", 1U).Returns(false);

    async Task Because() => _result = await _command.Handle(_storage, _manager);

    [Fact] void should_report_unavailable_even_for_identical_content() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
