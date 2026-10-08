// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_sql_cannot_rule_out_historical_revisions : given.a_sql_event
{
    async Task Because() => _result = await Verify();

    [Fact] void should_fail_closed_without_revision_tracking() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
