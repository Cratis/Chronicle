// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_migrated_protected_string_is_lossless : given.a_migrated_formatted_protected_value
{
    async Task Establish() => await Store(
        """{"type":"object","properties":{"value":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""",
        """{"value":"A protected string"}""");

    async Task Because() => _result = await Verify();

    [Fact] void should_compare_the_lossless_direct_string_conversion() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
