// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_sql_left_the_generation_unrevised : given.a_sql_event
{
    async Task Establish()
    {
        await Revise();
        _command = _command with { EventType = new("event", 2, false), Content = "{\"renamed\":42}" };
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_keep_the_content_the_generation_was_appended_with() => _stored.GenerationalContent[2].ShouldContain("\"renamed\":42");
    [Fact] void should_serve_the_revision_as_the_current_content() => _stored.Revisions.Last().Content.ShouldContain("\"value\":43");
    [Fact] void should_not_claim_equality_for_a_revised_event() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
