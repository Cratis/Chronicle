// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_performing;

public class and_child_object_has_item_level_metadata : Specification
{
    JsonSchema _schema;
    ReadModelsCompliance _compliance;
    EncryptChangeset _step;
    IProjection _projection;
    ProjectionEventContext _context;
    ExpandoObject _child;
    ExpandoObject _released;

    void Establish()
    {
        _schema = JsonSchema.FromJson(
            """
            {
              "type": "object",
              "properties": {
                "contacts": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "compliance": [{ "metadataType": "PII", "details": "" }],
                    "properties": { "name": { "type": "string" }, "contactId": { "type": "string" } }
                  }
                }
              }
            }
            """);
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
        var manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _compliance = new(manager, new ExpandoObjectConverter(new TypeFormats()));
        var comparer = new ObjectComparer();
        _step = new(_compliance, comparer, "test-store", "test-namespace");
        _projection = Substitute.For<IProjection>();
        _projection.TargetReadModelSchema.Returns(_schema);
        var @event = new AppendedEvent(
            EventContext.From("test-store", "test-namespace", EventType.Unknown, EventSourceType.Default, "owner", EventStreamType.All, EventStreamId.Default, EventSequenceNumber.First, CorrelationId.NotSet),
            new ExpandoObject());
        _child = new ExpandoObject();
        var childValues = (IDictionary<string, object?>)_child;
        childValues["name"] = "Jane";
        childValues["contactId"] = "contact-1";
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(comparer, @event, new ExpandoObject());
        changeset.AddChild("contacts", _child);
        _context = new(new Key("owner", ArrayIndexers.NoIndexers), @event, changeset, ProjectionOperationType.None, false);
    }

    async Task Because()
    {
        await _step.Perform(_projection, _context);
        // The child payload is what the sink persists through $push. Read that exact representation,
        // rather than the independent root snapshot, to pin child write/read symmetry.
        var persisted = new ExpandoObject();
        var values = (IDictionary<string, object?>)persisted;
        values["contacts"] = new[] { _child };
        values[WellKnownProperties.Subject] = "owner";
        _released = await _compliance.Release("test-store", "test-namespace", _schema, persisted);
    }

    [Fact] void should_protect_the_child_string_member() => ((IDictionary<string, object?>)_child)["name"].ShouldNotEqual("Jane");
    [Fact] void should_protect_every_declared_child_member() => ((IDictionary<string, object?>)_child)["contactId"].ShouldNotEqual("contact-1");
    [Fact] void should_release_the_child_string_member() => ReleasedChild()["name"].ShouldEqual("Jane");
    [Fact] void should_release_every_declared_child_member() => ReleasedChild()["contactId"].ShouldEqual("contact-1");

    IDictionary<string, object?> ReleasedChild() => (IDictionary<string, object?>)((IEnumerable<object>)((IDictionary<string, object?>)_released)["contacts"]!).Single();
}
