// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_performing;

public class and_projection_is_rekeyed : given.all_dependencies
{
    const string ResolvedKey = "resolved-key";

    ProjectionEventContext _context;
    ProjectionEventContext _result;

    void Establish()
    {
        _schema.Properties["name"] = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
            }
        };

        // A re-keyed projection: the document key (resolved key) differs from the source event's event source id.
        _context = CreateContext(EventSourceIdValue, key: ResolvedKey);
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_use_the_resolved_key_as_identifier() => _complianceManager.Received(1).Apply(EventStore, EventStoreNamespace, Arg.Any<JsonSchema>(), ResolvedKey, Arg.Any<JsonObject>());
    [Fact] void should_not_use_the_event_source_id() => _complianceManager.DidNotReceive().Apply(EventStore, EventStoreNamespace, Arg.Any<JsonSchema>(), EventSourceIdValue, Arg.Any<JsonObject>());
}
