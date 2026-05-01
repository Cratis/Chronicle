// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instances;

/// <summary>
/// Spec verifying that PII fields are decrypted before returning instances.
/// </summary>
public class and_read_model_has_pii_property : given.all_dependencies
{
    GetInstancesResponse _result;
    ExpandoObject _encryptedInstance;

    void Establish()
    {
        var property = new JsonSchemaProperty
        {
            ExtensionData = new Dictionary<string, object?>
            {
                { ComplianceJsonSchemaExtensions.ComplianceKey, new[] { new ComplianceSchemaMetadata(Guid.NewGuid(), string.Empty) } }
            }
        };

        _readModelDefinition = _readModelDefinition with
        {
            Schemas = new Dictionary<Concepts.ReadModels.ReadModelGeneration, JsonSchema>
            {
                {
                    (Concepts.ReadModels.ReadModelGeneration)1,
                    new JsonSchema { Properties = { ["name"] = property } }
                }
            }
        };

        _readModel.GetDefinition().Returns(Task.FromResult(_readModelDefinition));

        _encryptedInstance = new ExpandoObject();
        ((IDictionary<string, object?>)_encryptedInstance)[WellKnownProperties.Subject] = "some-subject";
        ((IDictionary<string, object?>)_encryptedInstance)["name"] = "encrypted-value";

        _sink.GetInstances(Arg.Any<Concepts.ReadModels.ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ReadModelInstances([_encryptedInstance], 1));

        _complianceManager.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(ci => Task.FromResult(new JsonObject { ["name"] = "decrypted-value" }));
    }

    async Task Because() => _result = await _service.GetInstances(new GetInstancesRequest
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModel = "test-read-model",
        Page = 0,
        PageSize = 20
    });

    [Fact] void should_call_compliance_manager_release() => _complianceManager.Received(1).Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), "some-subject", Arg.Any<JsonObject>());
    [Fact] void should_return_one_instance() => _result.Instances.Count.ShouldEqual(1);
}
