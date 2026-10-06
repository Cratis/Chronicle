// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_the_immediate_projection_subject_differs_from_the_request_key : given.all_dependencies
{
    const string Owner = "owner";
    const string Source = "different-source";
    const string Plaintext = "Ada Lovelace";
    protected GetInstanceByKeyResponse _result;
    GetInstanceByKeyResponse _afterErasure;
    InMemoryEncryptionKeyStorage _keyStorage;

    protected virtual string IdentifierProperty => "Id";
    protected virtual string SessionId => string.Empty;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync(
            """
            { "type": "object", "properties": {
              "Id": { "type": "string" },
              "name": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
              "otherName": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
              "subjectSecret": { "type": "string", "security": [{ "metadataType": "EncryptedSubject", "details": "" }] },
              "namespaceSecret": { "type": "string", "security": [{ "metadataType": "EncryptedNamespace", "details": "" }] },
              "globalSecret": { "type": "string", "security": [{ "metadataType": "EncryptedGlobal", "details": "" }] }
            } }
            """.Replace("\"Id\"", $"\"{IdentifierProperty}\"", StringComparison.Ordinal));

        _readModelDefinition = _readModelDefinition with
        {
            Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, schema } }
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);
        _sink.TypeId.Returns(SinkTypeId.None);

        _keyStorage = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keyStorage, encryption);
        var manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
                new PIICompliancePropertyValueHandler(provisioner, _keyStorage, encryption),
                new EncryptedSubjectValueHandler(provisioner, _keyStorage, encryption),
                new EncryptedNamespaceValueHandler(provisioner, _keyStorage, encryption),
                new EncryptedGlobalValueHandler(provisioner, _keyStorage, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        var projected = await manager.Apply("test-store", "test-namespace", schema, Owner, new JsonObject { [IdentifierProperty] = Source, ["name"] = Plaintext, ["subjectSecret"] = "subject secret", ["namespaceSecret"] = "namespace secret", ["globalSecret"] = "global secret" });
        var otherContribution = await manager.Apply("test-store", "test-namespace", schema, "other-owner", new JsonObject { ["otherName"] = "other name" });
        projected["otherName"] = otherContribution["otherName"]!.DeepClone();
        projected[WellKnownProperties.Subjects] = new JsonObject { ["otherName"] = "other-owner" };

        // Even a producer that supplies the authoritative owner has it overwritten by the request key.
        projected[WellKnownProperties.Subject] = Owner;
        var immediateProjection = Substitute.For<IImmediateProjection>();
        immediateProjection.GetModelInstance().Returns(new ProjectionResult(projected, 1, (EventSequenceNumber)42));
        _grainFactory.GetGrain<IImmediateProjection>(Arg.Any<string>()).Returns(immediateProjection);

        _complianceHelper = new ReadModelsCompliance(manager, new ExpandoObjectConverter(new TypeFormats()));
        _service = new ReadModels(_grainFactory, _storage, _expandoObjectConverter, _reducerMediator, _changesetMediator, _localSiloDetails, _complianceHelper, _materializedReadModels, new JsonSerializerOptions());
    }

    async Task Because()
    {
        var request = new GetInstanceByKeyRequest
        {
            EventStore = "test-store",
            Namespace = "test-namespace",
            ReadModelIdentifier = _readModelDefinition.Identifier,
            EventSequenceId = "event-log",
            ReadModelKey = Source,
            SessionId = SessionId
        };
        _result = await _service.GetInstanceByKey(request);
        await _keyStorage.RecordErasureFor("test-store", "test-namespace", Owner);
        await _keyStorage.DeleteFor("test-store", "test-namespace", Owner);
        _afterErasure = await _service.GetInstanceByKey(request);
    }

    [Fact] void should_release_using_the_owner_instead_of_the_request_key() => JsonNode.Parse(_result.ReadModel)!["name"]!.GetValue<string>().ShouldEqual(Plaintext);
    [Fact] void should_release_the_other_contributing_subject() => JsonNode.Parse(_result.ReadModel)!["otherName"]!.GetValue<string>().ShouldEqual("other name");
    [Fact] void should_erase_only_the_personal_data_for_the_erased_owner() => JsonNode.Parse(_afterErasure.ReadModel)!["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_keep_the_other_owners_personal_data() => JsonNode.Parse(_afterErasure.ReadModel)!["otherName"]!.GetValue<string>().ShouldEqual("other name");
    [Fact] void should_keep_subject_confidentiality_after_erasure() => JsonNode.Parse(_afterErasure.ReadModel)!["subjectSecret"]!.GetValue<string>().ShouldEqual("subject secret");
    [Fact] void should_keep_namespace_confidentiality_after_erasure() => JsonNode.Parse(_afterErasure.ReadModel)!["namespaceSecret"]!.GetValue<string>().ShouldEqual("namespace secret");
    [Fact] void should_keep_global_confidentiality_after_erasure() => JsonNode.Parse(_afterErasure.ReadModel)!["globalSecret"]!.GetValue<string>().ShouldEqual("global secret");
    [Fact] void should_strip_subject_metadata_from_the_reply() => JsonNode.Parse(_result.ReadModel)!.AsObject().ContainsKey(WellKnownProperties.Subjects).ShouldBeFalse();
}
