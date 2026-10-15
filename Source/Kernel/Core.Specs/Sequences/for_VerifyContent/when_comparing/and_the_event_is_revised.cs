// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_event_is_revised : given.a_stored_event
{
    void Establish() => _stored = _stored with
    {
        Revisions = [new(1, CorrelationId.New(), [], Concepts.Identities.Identity.System, DateTimeOffset.UtcNow, _command.Content)]
    };

    async Task Because() => _result = await Verify();

    [Fact] void should_not_compare_original_content_as_the_revision() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
