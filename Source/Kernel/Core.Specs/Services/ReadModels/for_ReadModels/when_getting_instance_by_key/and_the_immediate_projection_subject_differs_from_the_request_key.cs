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
    GetInstanceByKeyResponse _result;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync(
            """
            { "type": "object", "properties": {
              "Id": { "type": "string" },
              "name": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] }
            } }
            """);
        _readModelDefinition = _readModelDefinition with
        {
            Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, schema } }
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);
        _sink.TypeId.Returns(SinkTypeId.None);

        var keyStorage = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(keyStorage, encryption);
        var manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, keyStorage, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        var projected = await manager.Apply("test-store", "test-namespace", schema, Owner, new JsonObject { ["Id"] = Source, ["name"] = Plaintext });

        // Even a producer that supplies the authoritative owner has it overwritten by the request key.
        projected[WellKnownProperties.Subject] = Owner;
        var immediateProjection = Substitute.For<IImmediateProjection>();
        immediateProjection.GetModelInstance().Returns(new ProjectionResult(projected, 1, (EventSequenceNumber)42));
        _grainFactory.GetGrain<IImmediateProjection>(Arg.Any<string>()).Returns(immediateProjection);

        _complianceHelper = new ReadModelsCompliance(manager, new ExpandoObjectConverter(new TypeFormats()));
        _service = new ReadModels(_grainFactory, _storage, _expandoObjectConverter, _reducerMediator, _changesetMediator, _localSiloDetails, _complianceHelper, _materializedReadModels, new JsonSerializerOptions());
    }

    async Task Because() => _result = await _service.GetInstanceByKey(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        ReadModelKey = Source
    });

    [Fact] void should_release_using_the_owner_instead_of_the_request_key() => JsonNode.Parse(_result.ReadModel)!["name"]!.GetValue<string>().ShouldEqual(Plaintext);
}
