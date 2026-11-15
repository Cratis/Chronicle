// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_with_status;

public class and_the_schema_has_no_metadata : given.a_manager
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"number":{"type":"integer"},"empty":{"type":"string"}}}""");
        _source = new() { ["number"] = 0, ["empty"] = string.Empty };
    }

    async Task Because() => _result = await _manager.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _source);

    [Fact] void should_preserve_content() => JsonNode.DeepEquals(_result.Content, _source).ShouldBeTrue();
    [Fact] void should_return_no_protected_statuses() => _result.Statuses.Count.ShouldEqual(0);
    [Fact] void should_return_a_clone() => ReferenceEquals(_result.Content, _source).ShouldBeFalse();
}
