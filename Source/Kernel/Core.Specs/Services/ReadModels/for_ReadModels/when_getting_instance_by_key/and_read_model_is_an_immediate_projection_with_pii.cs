// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_read_model_is_an_immediate_projection_with_pii : given.all_dependencies
{
    const string Subject = "read-model-key";

    GetInstanceByKeyResponse _result = null!;

    void Establish()
    {
        var schema = new JsonSchema
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
            }
        };

        _readModelDefinition = _readModelDefinition with
        {
            Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, schema } }
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);

        // Passive sink → the lookup falls through to the immediate projection.
        _sink.TypeId.Returns(SinkTypeId.None);

        var immediateProjection = Substitute.For<IImmediateProjection>();
        immediateProjection.GetModelInstance().Returns(new ProjectionResult(
            new JsonObject { ["name"] = "encrypted-value", ["_id"] = Subject },
            1,
            (EventSequenceNumber)42));
        _grainFactory.GetGrain<IImmediateProjection>(Arg.Any<string>()).Returns(immediateProjection);

        // The compliance helper decrypts the projected read model.
        _complianceHelper.ReleaseJson(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<JsonObject>())
            .Returns(new JsonObject { ["name"] = "decrypted-value", ["_id"] = Subject });
    }

    async Task Because() => _result = await _service.GetInstanceByKey(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        ReadModelKey = Subject
    });

    [Fact] void should_release_compliance_metadata() => _complianceHelper.Received(1).ReleaseJson(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<JsonObject>());
    [Fact] void should_return_the_decrypted_read_model() => JsonSerializer.Deserialize<JsonElement>(_result.ReadModel).GetProperty("name").GetString().ShouldEqual("decrypted-value");
    [Fact] void should_not_leak_the_subject_marker() => JsonSerializer.Deserialize<JsonElement>(_result.ReadModel).TryGetProperty(WellKnownProperties.Subject, out _).ShouldBeFalse();
}
