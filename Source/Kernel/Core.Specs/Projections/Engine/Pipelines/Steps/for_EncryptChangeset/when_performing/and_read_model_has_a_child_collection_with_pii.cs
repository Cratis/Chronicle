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
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_performing;

public class and_read_model_has_a_child_collection_with_pii : Specification
{
    const string PlaintextName = "Jane Doe";

    EncryptChangeset _step;
    IProjection _projection;
    ProjectionEventContext _context;
    ExpandoObject _child;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "contacts": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "contactId": { "type": "string" },
                      "name": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] },
                      "status": { "type": "string" }
                    }
                  }
                }
              }
            }
            """);

        var typeFormats = new TypeFormats();
        var complianceManager = new JsonComplianceManager(
            new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(
                new PIICompliancePropertyValueHandler(new InMemoryEncryptionKeyStorage(), new Encryption())));
        var compliance = new ReadModelsCompliance(complianceManager, new ExpandoObjectConverter(typeFormats));
        var objectComparer = new ObjectComparer();
        _step = new EncryptChangeset(compliance, objectComparer, "test-store", "test-namespace");

        _projection = Substitute.For<IProjection>();
        _projection.TargetReadModelSchema.Returns(schema);

        var @event = new AppendedEvent(
            EventContext.From(
                "test-store",
                "test-namespace",
                EventType.Unknown,
                EventSourceType.Default,
                "owner-1",
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.First,
                CorrelationId.NotSet),
            new ExpandoObject());

        _child = new ExpandoObject();
        var childValues = (IDictionary<string, object?>)_child;
        childValues["contactId"] = "contact-1";
        childValues["name"] = PlaintextName;
        childValues["status"] = "active";

        var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, @event, new ExpandoObject());
        changeset.AddChild("contacts", _child);
        _context = new ProjectionEventContext(new Key("owner-1", ArrayIndexers.NoIndexers), @event, changeset, ProjectionOperationType.None, false);
    }

    async Task Because() => await _step.Perform(_projection, _context);

    [Fact] void should_encrypt_the_child_pii_member() => ChildValue("name").ShouldNotEqual(PlaintextName);

    [Fact] void should_store_the_child_pii_member_as_base64_ciphertext() => IsBase64(ChildValue("name")).ShouldBeTrue();

    [Fact] void should_leave_non_pii_child_members_untouched() => ChildValue("status").ShouldEqual("active");

    string ChildValue(string property) => (string)((IDictionary<string, object?>)_child)[property]!;

    static bool IsBase64(string value)
    {
        Span<byte> buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out _);
    }
}
