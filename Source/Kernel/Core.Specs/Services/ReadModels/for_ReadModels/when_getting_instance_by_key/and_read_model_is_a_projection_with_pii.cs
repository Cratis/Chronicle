// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instance_by_key;

public class and_read_model_is_a_projection_with_pii : given.all_dependencies
{
    const string Subject = "read-model-key";

    GetInstanceByKeyResponse _result = null!;
    JsonSchema _schema;

    void Establish()
    {
        _schema = new JsonSchema
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata("PII", string.Empty) } }
            }
        };

        _readModelDefinition = _readModelDefinition with
        {
            Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, _schema } }
        };
        _readModel.GetDefinition().Returns(_readModelDefinition);

        var storedState = new ExpandoObject();
        var storedDict = (IDictionary<string, object?>)storedState;
        storedDict["name"] = "encrypted-value";
        storedDict[WellKnownProperties.Subject] = Subject;

        _sink.TypeId.Returns(WellKnownSinkTypes.InMemory);
        _sink.FindOrDefault(Arg.Any<Key>()).Returns(storedState);

        // The compliance helper decrypts the stored read model.
        var decrypted = new ExpandoObject();
        ((IDictionary<string, object?>)decrypted)["name"] = "decrypted-value";
        _complianceHelper.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<ExpandoObject>())
            .Returns(decrypted);

        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call =>
            {
                var jsonObject = new JsonObject();
                foreach (var (key, value) in (IDictionary<string, object?>)call.Arg<ExpandoObject>())
                {
                    jsonObject[key] = value?.ToString();
                }

                return jsonObject;
            });
    }

    async Task Because() => _result = await _service.GetInstanceByKey(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        ReadModelKey = Subject
    });

    [Fact] void should_release_compliance_metadata() => _complianceHelper.Received(1).Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<ExpandoObject>());
    [Fact] void should_return_the_decrypted_read_model() => JsonSerializer.Deserialize<JsonElement>(_result.ReadModel).GetProperty("name").GetString().ShouldEqual("decrypted-value");
}
