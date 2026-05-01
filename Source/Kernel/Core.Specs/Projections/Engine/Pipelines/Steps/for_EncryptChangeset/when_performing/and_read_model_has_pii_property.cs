// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_performing;

public class and_read_model_has_pii_property : given.all_dependencies
{
    ProjectionEventContext _context;
    ProjectionEventContext _result;

    void Establish()
    {
        var property = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata(Guid.NewGuid(), string.Empty) } }
            }
        };
        _schema.Properties["name"] = property;
        _context = CreateContext(EventSourceIdValue);
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_call_apply_with_event_source_id_as_identifier() => _complianceManager.Received(1).Apply(EventStore, EventStoreNamespace, Arg.Any<JsonSchema>(), EventSourceIdValue, Arg.Any<JsonObject>());
}
