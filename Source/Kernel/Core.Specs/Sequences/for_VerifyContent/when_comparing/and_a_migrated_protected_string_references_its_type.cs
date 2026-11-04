// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_migrated_protected_string_references_its_type : given.a_migrated_formatted_protected_value
{
    async Task Establish() => await Store(
        """{"type":"object","properties":{"value":{"$ref":"#/definitions/value","compliance":[{"metadataType":"PII","details":""}]}},"definitions":{"value":{"type":"string"}}}""",
        """{"value":"A protected string"}""");

    async Task Because() => _result = await Verify();

    [Fact] void should_compare_the_lossless_referenced_string_conversion() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
