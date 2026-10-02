// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_offsets_differ_in_memory : given.a_storage_round_trip
{
    async Task Establish()
    {
        await Store(OffsetSchema, """{"value":"2026-03-04T05:06:07+02:30"}""", sql: false);
        _command = _command with { Content = """{"value":"2026-03-04T02:36:07+00:00"}""" };
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_distinguish_offsets_for_the_same_instant() => _result.Result.ShouldEqual(ContentVerificationResult.Different);
}
