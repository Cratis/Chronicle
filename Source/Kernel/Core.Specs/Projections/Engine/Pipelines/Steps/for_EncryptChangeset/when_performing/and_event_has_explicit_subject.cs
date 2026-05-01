// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_performing;

public class and_event_has_explicit_subject : given.all_dependencies
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
        _context = CreateContext(EventSourceIdValue, (Subject)SubjectValue);
    }

    async Task Because() => _result = await _step.Perform(_projection, _context);

    [Fact] void should_use_subject_as_identifier() => _complianceManager.Received(1).Apply(EventStore, EventStoreNamespace, Arg.Any<JsonSchema>(), SubjectValue, Arg.Any<JsonObject>());
    [Fact] void should_not_use_event_source_id() => _complianceManager.DidNotReceive().Apply(EventStore, EventStoreNamespace, Arg.Any<JsonSchema>(), EventSourceIdValue, Arg.Any<JsonObject>());
}
