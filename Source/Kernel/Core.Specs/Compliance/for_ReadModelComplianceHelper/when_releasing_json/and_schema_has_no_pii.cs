// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance.for_ReadModelComplianceHelper.when_releasing_json;

public class and_schema_has_no_pii : given.all_dependencies
{
    JsonObject _instance;
    JsonObject _result;

    void Establish() => _instance = new JsonObject { ["value"] = "test-value" };

    async Task Because() => _result = await ReadModelComplianceHelper.ReleaseJson(
        _complianceManager,
        EventStore,
        EventStoreNamespace,
        _schemaWithoutPii,
        _instance);

    [Fact] void should_return_original_instance() => _result.ShouldEqual(_instance);
    [Fact] void should_not_call_compliance_manager() => _complianceManager.DidNotReceive().Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
}
