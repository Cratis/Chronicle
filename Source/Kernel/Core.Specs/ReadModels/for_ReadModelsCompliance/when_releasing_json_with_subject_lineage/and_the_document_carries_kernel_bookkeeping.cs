// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_releasing_json_with_subject_lineage;

/// <summary>Undeclared bookkeeping never reaches the manager; declared bookkeeping survives.</summary>
public class and_the_document_carries_kernel_bookkeeping : given.all_dependencies
{
    JsonObject _handedToComplianceManager;

    void Establish()
    {
        _schemaWithPii.Properties[WellKnownProperties.LastHandledEventSequenceNumber] = new JsonSchemaProperty(
            WellKnownProperties.LastHandledEventSequenceNumber,
            new JsonObject { ["type"] = "integer" },
            _schemaWithPii);

        _complianceManager.Release(
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Any<JsonSchema>(),
                Arg.Any<string>(),
                Arg.Any<JsonObject>())
            .Returns(callInfo =>
            {
                _handedToComplianceManager = callInfo.ArgAt<JsonObject>(4);
                return Task.FromResult(new JsonObject { ["name"] = "decrypted-name" });
            });
    }

    async Task Because() => await _compliance.ReleaseJson(
        EventStore,
        EventStoreNamespace,
        _schemaWithPii,
        new JsonObject
        {
            ["name"] = "encrypted-name",
            [WellKnownProperties.LastHandledEventSequenceNumber] = JsonValue.Create(42UL),
            [WellKnownProperties.Subject] = "subject-1",
            [WellKnownProperties.ReadModelInstanceInitialized] = true
        },
        "subject-1",
        new Dictionary<string, string>());

    [Fact] void should_keep_the_declared_property() => _handedToComplianceManager[WellKnownProperties.LastHandledEventSequenceNumber].GetValue<ulong>().ShouldEqual(42UL);
    [Fact] void should_not_hand_over_the_subject() => _handedToComplianceManager.ContainsKey(WellKnownProperties.Subject).ShouldBeFalse();
    [Fact] void should_not_hand_over_the_initialization_flag() => _handedToComplianceManager.ContainsKey(WellKnownProperties.ReadModelInstanceInitialized).ShouldBeFalse();
}
