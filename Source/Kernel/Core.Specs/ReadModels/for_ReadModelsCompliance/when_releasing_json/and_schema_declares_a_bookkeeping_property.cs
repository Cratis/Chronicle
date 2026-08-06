// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_releasing_json;

/// <summary>
/// A read model may expose the sink watermark as a property of its own, and several do. Once the schema declares
/// it, it is a property like any other: the compliance manager accepts it, so stripping it would drop data the
/// read model asked for.
/// </summary>
public class and_schema_declares_a_bookkeeping_property : given.all_dependencies
{
    JsonObject _instance;
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

        _instance = new JsonObject
        {
            [WellKnownProperties.Subject] = Identifier,
            [WellKnownProperties.LastHandledEventSequenceNumber] = JsonValue.Create(42UL),
            ["name"] = "encrypted-name"
        };
    }

    async Task Because() => await _compliance.ReleaseJson(
        EventStore,
        EventStoreNamespace,
        _schemaWithPii,
        _instance);

    [Fact] void should_keep_the_declared_property() => _handedToComplianceManager[WellKnownProperties.LastHandledEventSequenceNumber]!.GetValue<ulong>().ShouldEqual(42UL);
    [Fact] void should_still_strip_the_subject_marker() => _handedToComplianceManager.ContainsKey(WellKnownProperties.Subject).ShouldBeFalse();
}
