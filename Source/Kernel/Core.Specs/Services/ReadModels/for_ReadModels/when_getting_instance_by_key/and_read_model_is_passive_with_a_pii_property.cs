// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

/// <summary>
/// A passive read model has no sink, so every keyed read falls through to the immediate projection and the
/// projected document is released as JSON rather than through a schema round trip. That path stamps the internal
/// subject marker onto the document so the compliance manager knows whose key to decrypt with, and the compliance
/// manager walks every property it is handed against the read model schema — which declares no such marker. The
/// compliance chain is wired for real here rather than substituted, because a substitute at that seam is exactly
/// what hides the hand-off that fails.
/// </summary>
public class and_read_model_is_passive_with_a_pii_property : given.all_dependencies
{
    const string Key = "person-42";
    const string EncryptedName = "encrypted-name";
    const string DecryptedName = "decrypted-name";

    GetInstanceByKeyResponse _result;
    Exception _exception;

    void Establish()
    {
        var schema = JsonSchema.FromType<ReadModelWithPersonalData>();
        schema.Properties["name"].ExtensionData = new Dictionary<string, object?>
        {
            { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
        };

        _readModelDefinition = _readModelDefinition with
        {
            Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, schema } }
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);

        // Passive read model — the sink never holds data, so the lookup falls through to the immediate projection.
        _sink.TypeId.Returns(SinkTypeId.None);

        var immediateProjection = Substitute.For<IImmediateProjection>();
        immediateProjection.GetModelInstance().Returns(new ProjectionResult(
            new JsonObject { ["id"] = Key, ["name"] = EncryptedName },
            1,
            (EventSequenceNumber)42));
        _grainFactory.GetGrain<IImmediateProjection>(Arg.Any<string>()).Returns(immediateProjection);

        var valueHandler = Substitute.For<IJsonCompliancePropertyValueHandler>();
        valueHandler.Type.Returns((ComplianceMetadataType)"PII");
        valueHandler.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Key, Arg.Any<JsonNode>())
            .Returns(Task.FromResult<JsonNode>(JsonValue.Create(DecryptedName)));

        _service = new ReadModels(
            _grainFactory,
            _storage,
            _expandoObjectConverter,
            _reducerMediator,
            _changesetMediator,
            _localSiloDetails,
            new ReadModelsCompliance(
                new JsonComplianceManager(
                    new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(valueHandler),
                    NullLogger<JsonComplianceManager>.Instance),
                _expandoObjectConverter),
            _eventCompliance,
            _materializedReadModels,
            new JsonSerializerOptions());
    }

    async Task Because() => _exception = await Catch.Exception(async () => _result = await _service.GetInstanceByKey(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        ReadModelKey = Key
    }));

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_return_the_decrypted_read_model() => JsonSerializer.Deserialize<JsonElement>(_result.ReadModel).GetProperty("name").GetString().ShouldEqual(DecryptedName);
    [Fact] void should_not_leak_the_subject_marker() => JsonSerializer.Deserialize<JsonElement>(_result.ReadModel).TryGetProperty(WellKnownProperties.Subject, out _).ShouldBeFalse();

    record ReadModelWithPersonalData(string Id, string Name);
}
