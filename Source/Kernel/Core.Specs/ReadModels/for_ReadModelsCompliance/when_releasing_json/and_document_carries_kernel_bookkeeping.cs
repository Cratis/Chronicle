// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_releasing_json;

/// <summary>
/// The compliance manager rejects every property the read model schema does not declare, so the whole of the
/// kernel's own bookkeeping has to come off the document before it is handed over — not just the subject marker
/// this method reads the identifier from.
/// </summary>
public class and_document_carries_kernel_bookkeeping : given.all_dependencies
{
    JsonObject _instance;
    string _instanceBefore;
    JsonObject _handedToComplianceManager;

    void Establish()
    {
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
            [WellKnownProperties.ReadModelInstanceInitialized] = true,
            ["name"] = "encrypted-name"
        };
        _instanceBefore = _instance.ToJsonString();
    }

    async Task Because() => await _compliance.ReleaseJson(
        EventStore,
        EventStoreNamespace,
        _schemaWithPii,
        _instance);

    [Fact] void should_hand_over_the_schema_declared_properties() => _handedToComplianceManager.Select(_ => _.Key).ShouldContainOnly(["name"]);
    [Fact] void should_leave_the_document_it_was_given_unchanged() => _instance.ToJsonString().ShouldEqual(_instanceBefore);
}
