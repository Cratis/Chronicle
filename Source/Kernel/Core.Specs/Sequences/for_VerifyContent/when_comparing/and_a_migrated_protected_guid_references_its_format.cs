// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_migrated_protected_guid_references_its_format : given.a_migrated_formatted_protected_value
{
    async Task Establish() => await Store(
        """{"type":"object","properties":{"value":{"$ref":"#/definitions/id","compliance":[{"metadataType":"PII","details":""}]}},"definitions":{"id":{"type":"string","format":"guid"}}}""",
        """{"value":"ABCDEFAB-1234-5678-9ABC-DEF012345678"}""");

    async Task Because() => _result = await Verify();

    [Fact] void should_loss_check_the_referenced_target_format() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
