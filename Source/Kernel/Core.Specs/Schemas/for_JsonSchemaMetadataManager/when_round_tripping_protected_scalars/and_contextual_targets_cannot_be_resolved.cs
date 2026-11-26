// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_round_tripping_protected_scalars;

public class and_contextual_targets_cannot_be_resolved : given.real_pii_protection
{
    Exception _dynamicError;
    Exception _recursiveError;
    JsonObject _unclassified;
    StatusedRelease _unclassifiedStatus;

    protected override IEncryption CreateEncryption() => LegacyEncryption.Create();

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {
              "type":"object",
              "properties":{
                "dynamic":{"$dynamicRef":"https://unavailable.example/personal","type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "recursive":{"$recursiveRef":"https://unavailable.example/personal","type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "unclassifiedDynamic":{"$dynamicRef":"https://unavailable.example/personal"},
                "unclassifiedRecursive":{"$recursiveRef":"https://unavailable.example/personal"}
              }
            }
            """);
        _plaintext = new() { ["dynamic"] = "sensitive", ["recursive"] = "sensitive" };
        await Protect();
    }

    async Task Because()
    {
        _dynamicError = await Catch.Exception(() => _manager.ReleaseWithStatus("store", "tenant", _schema, "subject", new JsonObject { ["dynamic"] = _protected["dynamic"]!.DeepClone() }));
        _recursiveError = await Catch.Exception(() => _manager.ReleaseWithStatus("store", "tenant", _schema, "subject", new JsonObject { ["recursive"] = _protected["recursive"]!.DeepClone() }));

        // With no real annotation there is no handler to invent. Existing ciphertext must remain opaque.
        var ciphertext = new JsonObject
        {
            ["unclassifiedDynamic"] = _protected["dynamic"]!.DeepClone(),
            ["unclassifiedRecursive"] = _protected["recursive"]!.DeepClone()
        };
        _unclassified = await _manager.Release("store", "tenant", _schema, "subject", ciphertext);
        _unclassifiedStatus = await _manager.ReleaseWithStatus("store", "tenant", _schema, "subject", ciphertext);
    }

    [Fact] void should_still_encrypt_the_explicitly_annotated_boundaries() => _protected.All(property => property.Value!.GetValue<string>() != "sensitive").ShouldBeTrue();
    [Fact] void should_refuse_unproven_dynamic_type_restoration() => _dynamicError.ShouldBeOfExactType<StrictSchemaMetadataReleaseNotSupported>();
    [Fact] void should_refuse_unproven_recursive_type_restoration() => _recursiveError.ShouldBeOfExactType<StrictSchemaMetadataReleaseNotSupported>();
    [Fact] void should_not_expose_plaintext_in_the_dynamic_failure() => _dynamicError.ToString().Contains("sensitive").ShouldBeFalse();
    [Fact] void should_not_expose_plaintext_in_the_recursive_failure() => _recursiveError.ToString().Contains("sensitive").ShouldBeFalse();
    [Fact] void should_not_leak_plaintext_from_unclassified_boundaries() => _unclassified.All(property => property.Value!.GetValue<string>() != "sensitive").ShouldBeTrue();
    [Fact] void should_keep_unclassified_ciphertext_unchanged_with_status() => JsonNode.DeepEquals(_unclassifiedStatus.Content, _unclassified).ShouldBeTrue();
    [Fact] void should_not_invent_release_statuses() => _unclassifiedStatus.Statuses.ShouldBeEmpty();
    [Fact] void should_not_invent_dynamic_handler_metadata() => _manager.MetadataFor(_schema.Properties["unclassifiedDynamic"], forComparison: true).ShouldBeEmpty();
    [Fact] void should_not_invent_recursive_handler_metadata() => _manager.MetadataFor(_schema.Properties["unclassifiedRecursive"], forComparison: true).ShouldBeEmpty();
}
