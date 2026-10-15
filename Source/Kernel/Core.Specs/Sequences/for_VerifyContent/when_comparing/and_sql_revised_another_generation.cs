// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_sql_revised_another_generation : given.a_sql_event
{
    async Task Establish()
    {
        await Revise();
        _command = _command with { EventType = new("event", 2, false), Content = "{\"renamed\":42}" };
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_not_claim_equality_with_a_stale_generation() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
