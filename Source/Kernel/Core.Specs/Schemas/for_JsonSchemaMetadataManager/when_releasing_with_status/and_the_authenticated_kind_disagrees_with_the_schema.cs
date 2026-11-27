// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_with_status;

public class and_the_authenticated_kind_disagrees_with_the_schema : given.a_manager
{
    const string Plaintext = "private-value-that-is-not-a-number";
    StatusedRelease _statused;
    JsonObject _ordinary;
    JsonObject _strict;
    JsonSchema _releaseSchema;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        _releaseSchema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"integer","compliance":[{"metadataType":"PII","details":""}]}}}""");
        _source = new JsonObject { ["value"] = Plaintext };
        _protected = await _manager.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _source);
    }

    async Task Because()
    {
        _statused = await _manager.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _releaseSchema, Identifier, _protected);
        _ordinary = await _manager.Release(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _releaseSchema, Identifier, _protected);
        _strict = await _manager.TryRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _releaseSchema, Identifier, _protected);
    }

    [Fact] void should_preserve_the_authenticated_string() => _statused.Content["value"].GetValue<string>().ShouldEqual(Plaintext);
    [Fact] void should_report_decrypted() => _statused.Statuses["value"].ShouldEqual(ProtectedValueReleaseStatus.Decrypted);
    [Fact] void should_bypass_ordinary_schema_coercion() => _ordinary["value"].GetValue<string>().ShouldEqual(Plaintext);
    [Fact] void should_bypass_strict_schema_coercion() => _strict["value"].GetValue<string>().ShouldEqual(Plaintext);
    [Fact] void should_not_add_provenance_to_json() => _statused.Content.Count.ShouldEqual(1);
}
