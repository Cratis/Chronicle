// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_only_the_attempted_protected_guid_is_noncanonical : given.a_migrated_formatted_protected_value
{
    async Task Establish() => await Store(
        """{"type":"object","properties":{"value":{"type":"string","format":"guid","compliance":[{"metadataType":"PII","details":""}]}}}""",
        """{"value":"ABCDEFAB-1234-5678-9ABC-DEF012345678"}""",
        attemptedGeneration: 2);

    async Task Because() => _result = await Verify();

    [Fact] void should_preserve_the_attempted_generation_protected_before_conversion() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
