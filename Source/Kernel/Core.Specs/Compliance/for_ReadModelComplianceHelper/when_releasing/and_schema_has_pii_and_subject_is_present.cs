// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Compliance.for_ReadModelComplianceHelper.when_releasing;

public class and_schema_has_pii_and_subject_is_present : given.all_dependencies
{
    ExpandoObject _result;

    void Establish() => ((IDictionary<string, object?>)_instance)[WellKnownProperties.Subject] = Identifier;

    async Task Because() => _result = await ReadModelComplianceHelper.Release(
        _complianceManager,
        EventStore,
        EventStoreNamespace,
        _schemaWithPii,
        _instance,
        _converter);

    [Fact] void should_call_compliance_manager_release() => _complianceManager.Received(1).Release(EventStore, EventStoreNamespace, _schemaWithPii, Identifier, Arg.Any<JsonObject>());
    [Fact] void should_return_decrypted_instance() => _result.ShouldNotBeNull();
}
