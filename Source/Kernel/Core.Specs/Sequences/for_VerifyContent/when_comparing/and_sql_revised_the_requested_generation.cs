// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_sql_revised_the_requested_generation : given.a_sql_event
{
    async Task Establish()
    {
        await Revise();
        _command = _command with { Content = "{\"value\":43}" };
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_not_claim_equality_with_untracked_revision_content() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
