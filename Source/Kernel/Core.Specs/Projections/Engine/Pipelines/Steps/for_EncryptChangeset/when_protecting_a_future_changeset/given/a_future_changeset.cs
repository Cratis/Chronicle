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

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_protecting_a_future_changeset.given;

public class a_future_changeset : Specification
{
    protected const string Email = "someone@example.com";
    protected readonly Guid _childId = Guid.Parse("d53be75c-2ac3-4e33-8f16-1a4e2f1066a0");
    protected EncryptChangeset _step;
    protected PIICompliancePropertyValueHandler _piiHandler;
    protected IProjection _projection;
    protected ProjectionEventContext _context;
    protected Changeset<AppendedEvent, ExpandoObject> _changeset;
    protected ExpandoObject _child;
    protected ExpandoObject _state;
    protected ArrayIndexers _indexers;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync(
            """
            { "type": "object", "properties": {
              "children": { "type": "array", "items": { "type": "object", "properties": {
                "id": { "type": "string", "format": "uuid" },
                "email": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
                "name": { "type": "string" }
              } } }
            } }
            """);
        var keyStorage = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(keyStorage, encryption);
        _piiHandler = new PIICompliancePropertyValueHandler(provisioner, keyStorage, encryption);
        var metadataManager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(_piiHandler),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        var compliance = new ReadModelsCompliance(metadataManager, new ExpandoObjectConverter(new TypeFormats()));
        _step = new EncryptChangeset(compliance, new ObjectComparer(), "test-store", "test-namespace");
        _projection = Substitute.For<IProjection>();
        _projection.TargetReadModelSchema.Returns(schema);
        var @event = new AppendedEvent(
            EventContext.From("test-store", "test-namespace", EventType.Unknown, EventSourceType.Default, "future-owner", EventStreamType.All, EventStreamId.Default, EventSequenceNumber.First, CorrelationId.NotSet),
            new ExpandoObject());
        _child = new ExpandoObject();
        var childValues = (IDictionary<string, object?>)_child;
        childValues["id"] = _childId;
        childValues["email"] = Email;
        childValues["name"] = "before";
        _state = new ExpandoObject();
        var stateValues = (IDictionary<string, object?>)_state;
        stateValues["children"] = new List<ExpandoObject> { _child };
        stateValues[WellKnownProperties.Subject] = "root-owner";
        _changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), @event, _state);
        _context = new ProjectionEventContext(new Key("future-owner", ArrayIndexers.NoIndexers), @event, _changeset, ProjectionOperationType.None, false);
        _indexers = new ArrayIndexers([new ArrayIndexer("[children]", "id", _childId)]);
    }

    protected void AddChild()
    {
        ((IDictionary<string, object?>)_state)["children"] = new List<ExpandoObject>();
        _changeset.AddChild<ExpandoObject>("[children]", "id", _childId, [], _indexers, _child);
        _child = (ExpandoObject)_changeset.Changes.OfType<ChildAdded>().Single().Child;
    }

    protected void UpdateChild()
    {
        _changeset.SetProperties(
            [
                (_, state, indexers) => Set(state, "[children].email", Email, indexers),
                (_, state, indexers) => Set(state, "[children].name", "after", indexers)
            ],
            _indexers);
    }

    protected IEnumerable<PropertyDifference> Differences => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().SelectMany(_ => _.Differences);

    static PropertyDifference Set(ExpandoObject state, PropertyPath path, string value, ArrayIndexers indexers)
    {
        var original = path.GetValue(state, indexers);
        path.SetValue(state, value, indexers);
        return new PropertyDifference(path, Equals(original, value) ? "previous" : original, value, indexers);
    }
}
