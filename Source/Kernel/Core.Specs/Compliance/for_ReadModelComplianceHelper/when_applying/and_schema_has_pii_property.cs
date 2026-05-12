// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Compliance.for_ReadModelComplianceHelper.when_applying;

public class and_schema_has_pii_property : given.all_dependencies
{
    ExpandoObject _result;

    async Task Because() => _result = await ReadModelComplianceHelper.Apply(
        _complianceManager,
        EventStore,
        EventStoreNamespace,
        _schemaWithPii,
        Identifier,
        _instance,
        _converter);

    [Fact] void should_call_compliance_manager_apply() => _complianceManager.Received(1).Apply(EventStore, EventStoreNamespace, _schemaWithPii, Identifier, Arg.Any<JsonObject>());
    [Fact] void should_write_subject_to_result() => ((IDictionary<string, object?>)_result).ContainsKey(WellKnownProperties.Subject).ShouldBeTrue();
    [Fact] void should_write_correct_subject_value() => ((IDictionary<string, object?>)_result)[WellKnownProperties.Subject].ShouldEqual(Identifier);
}
