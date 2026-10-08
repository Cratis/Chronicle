// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_mongodb_content_contains_large_unsigned_integers : given.a_mongodb_round_trip
{
    async Task Establish() => await StoreInMongoDB(
        """{"type":"object","properties":{"value":{"type":"integer","format":"uint64"},"values":{"type":"array","items":{"type":"integer","format":"uint64"}}}}""",
        """{"value":18446744073709551615,"values":[9223372036854775808,18446744073709551615]}""");

    async Task Because() => _result = await Verify();

    [Fact] void should_compare_without_narrowing_the_unsigned_integers() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
