// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_with_status;

public class and_two_handlers_claim_a_boundary : given.a_manager
{
    Exception _error;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
                "name":{"type":"string","compliance":[{"metadataType":"PII","details":""}],"security":[{"metadataType":"EncryptedGlobal","details":""}]}}}
            """);
        _source = JsonNode.Parse("""{"name":"someone"}""")!.AsObject();
    }

    async Task Because() => _error = await Catch.Exception(async () => await _manager.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _source));

    [Fact] void should_throw_unsupported_boundary() => _error.ShouldBeOfExactType<ProtectedBoundaryUnsupported>();
    [Fact] void should_name_the_path() => ((ProtectedBoundaryUnsupported)_error).Path.ShouldEqual("name");
}
