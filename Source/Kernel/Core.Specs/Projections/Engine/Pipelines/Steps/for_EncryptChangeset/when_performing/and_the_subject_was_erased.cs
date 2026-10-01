// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
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
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_EncryptChangeset.when_performing;

/// <summary>
/// #4453 through the projection pipeline's own steps: a stored read model holding a subject's personal data is
/// released by DecryptInitialState after the subject was erased, the projection changes a member that is not
/// personal, and EncryptChangeset protects the state before it is stored. That last step used to ask the erasure
/// fence for a new key and fail, freezing the partition.
/// </summary>
public class and_the_subject_was_erased : Specification
{
    const string EventStore = "test-store";
    const string Namespace = "test-namespace";
    const string Subject = "erased-subject";
    const string PlaintextName = "Ada Lovelace";

    InMemoryEncryptionKeyStorage _keyStorage;
    DecryptInitialState _decrypt;
    EncryptChangeset _encrypt;
    IProjection _projection;
    ProjectionEventContext _context;
    Exception? _exception;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "name": { "type": "string", "compliance": [ { "metadataType": "PII", "details": "" } ] },
                "address": {
                  "type": "object",
                  "properties": {
                    "postalCode": { "type": "integer", "format": "int32" },
                    "verified": { "type": "boolean" }
                  },
                  "compliance": [ { "metadataType": "PII", "details": "" } ]
                },
                "status": { "type": "string" }
              }
            }
            """);

        _keyStorage = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keyStorage, encryption);
        var complianceManager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, _keyStorage, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        var compliance = new ReadModelsCompliance(complianceManager, new ExpandoObjectConverter(new TypeFormats()));
        var objectComparer = new ObjectComparer();
        _decrypt = new DecryptInitialState(compliance, EventStore, Namespace);
        _encrypt = new EncryptChangeset(compliance, objectComparer, EventStore, Namespace);

        _projection = Substitute.For<IProjection>();
        _projection.TargetReadModelSchema.Returns(schema);

        dynamic address = new ExpandoObject();
        address.postalCode = 1337;
        address.verified = true;
        dynamic state = new ExpandoObject();
        state.name = PlaintextName;
        state.address = address;
        state.status = "registered";
        var stored = await compliance.Apply(EventStore, Namespace, schema, Subject, state);

        await _keyStorage.RecordErasureFor(EventStore, Namespace, Subject);
        await _keyStorage.DeleteFor(EventStore, Namespace, Subject);

        var @event = new AppendedEvent(
            EventContext.From(
                EventStore,
                Namespace,
                EventType.Unknown,
                EventSourceType.Default,
                Subject,
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.First,
                CorrelationId.NotSet),
            new ExpandoObject());

        var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, @event, stored);
        _context = new ProjectionEventContext(new Key(Subject, ArrayIndexers.NoIndexers), @event, changeset, ProjectionOperationType.None, false);
    }

    async Task Because()
    {
        try
        {
            await _decrypt.Perform(_projection, _context);
            _context.Changeset.SetProperties(
                [
                    (_, target, __) =>
                    {
                        ((IDictionary<string, object?>)target)["status"] = "moved";
                        return new PropertyDifference("status", "registered", "moved");
                    }
                ],
                ArrayIndexers.NoIndexers);
            await _encrypt.Perform(_projection, _context);
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
    }

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_store_the_change() => Serialized(StoredState).ShouldContain("\"status\":\"moved\"");
    [Fact] void should_store_the_name_as_erased() => ((IDictionary<string, object?>)StoredState)["name"].ShouldEqual(string.Empty);
    [Fact] void should_store_the_value_object_as_erased() => ((IDictionary<string, object?>)StoredState)["address"].ShouldEqual(string.Empty);
    [Fact] void should_store_no_personal_data() => Serialized(StoredState).ShouldNotContain(PlaintextName);
    [Fact] async Task should_not_provision_a_key() => (await _keyStorage.HasFor(EventStore, Namespace, Subject)).ShouldBeFalse();

    ExpandoObject StoredState => (ExpandoObject)_context.Changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().Last().State;

    static string Serialized(object value) => JsonSerializer.Serialize(value);
}
