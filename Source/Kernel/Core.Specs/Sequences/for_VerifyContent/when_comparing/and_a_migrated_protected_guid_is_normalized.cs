// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_migrated_protected_guid_is_normalized : given.a_migrated_formatted_protected_value
{
    async Task Establish() => await Store(
        """{"type":"object","properties":{"value":{"type":"string","format":"guid","compliance":[{"metadataType":"PII","details":""}]}}}""",
        """{"value":"ABCDEFAB-1234-5678-9ABC-DEF012345678"}""");

    async Task Because() => _result = await Verify();

    [Fact] void should_not_report_different_after_target_conversion_changes_the_value() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
