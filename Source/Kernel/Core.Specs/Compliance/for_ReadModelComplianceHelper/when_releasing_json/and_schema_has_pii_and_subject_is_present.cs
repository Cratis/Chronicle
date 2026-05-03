// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Compliance.for_ReadModelComplianceHelper.when_releasing_json;

public class and_schema_has_pii_and_subject_is_present : given.all_dependencies
{
    JsonObject _instance;
    JsonObject _result;

    void Establish() => _instance = new JsonObject
    {
        [WellKnownProperties.Subject] = Identifier,
        ["name"] = "encrypted-name"
    };

    async Task Because() => _result = await ReadModelComplianceHelper.ReleaseJson(
        _complianceManager,
        EventStore,
        EventStoreNamespace,
        _schemaWithPii,
        _instance);

    [Fact] void should_call_compliance_manager_release() => _complianceManager.Received(1).Release(EventStore, EventStoreNamespace, _schemaWithPii, Identifier, Arg.Any<JsonObject>());
    [Fact] void should_return_decrypted_instance() => _result.ShouldNotBeNull();
}
