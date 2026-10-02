// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_schema_conversion_loses_number_precision : given.a_storage_round_trip
{
    async Task Establish() => await Store("""{"type":"object","properties":{"value":{"type":"number"}}}""", """{"value":0.1234567890123456789012345678}""", sql: false);

    async Task Because() => _result = await Verify();

    [Fact] void should_not_claim_equality_for_a_rounded_attempt() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
