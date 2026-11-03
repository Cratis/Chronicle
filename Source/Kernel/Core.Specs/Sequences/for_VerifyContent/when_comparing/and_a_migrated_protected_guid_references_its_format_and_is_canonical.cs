// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.EventSequences.Migrations;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_a_migrated_protected_guid_references_its_format_and_is_canonical : given.a_migrated_formatted_protected_value
{
    async Task Establish() => await Store(
        """{"type":"object","properties":{"value":{"$ref":"#/definitions/id","compliance":[{"metadataType":"PII","details":""}]}},"definitions":{"id":{"type":"string","format":"guid"}}}""",
        """{"value":"abcdefab-1234-5678-9abc-def012345678"}""");

    async Task Because() => _result = await Verify();

    [Fact] void should_compare_the_lossless_referenced_target_conversion() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);

    [Fact]
    async Task should_preserve_the_canonical_value_through_the_referenced_conversion()
    {
        var schema = (await _storage.GetEventStore("store").EventTypes.GetFor("event", 2U)).Schema;
        var value = JsonNode.Parse("\"abcdefab-1234-5678-9abc-def012345678\"")!;
        var converted = GenerationContentConversion.ConvertProtectedValue(value, schema.Properties["value"], schema, _converter);
        converted.ShouldNotBeNull();
        converted.ToJsonString().ShouldEqual(value.ToJsonString());
        ContentComparison.Equals(value, converted).ShouldBeTrue();
        var released = GenerationContentConversion.ToReleasedValue(converted);
        released.ShouldNotBeNull();
        released.GetValue<string>().ShouldEqual(converted.ToString());
    }
}
