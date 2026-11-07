// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_with_status;

public class and_a_decrypted_value_has_the_wrong_kind : given.a_manager
{
    const string Plaintext = "secret-plaintext";
    Exception _error;
    JsonSchema _releaseSchema;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        _releaseSchema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"integer","compliance":[{"metadataType":"PII","details":""}]}}}""");
        _source = JsonNode.Parse("""{"value":"secret-plaintext"}""")!.AsObject();
        _protected = await _manager.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _source);
    }

    async Task Because() => _error = await Catch.Exception(async () => await _manager.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _releaseSchema, Identifier, _protected));

    [Fact] void should_throw_unsupported_release() => _error.ShouldBeOfExactType<StrictSchemaMetadataReleaseNotSupported>();
    [Fact] void should_not_reveal_the_plaintext() => _error.ToString().ShouldNotContain(Plaintext);
}
