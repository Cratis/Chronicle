// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Compliance.for_ReadModelComplianceHelper.when_applying;

public class and_schema_has_no_pii : given.all_dependencies
{
    ExpandoObject _result;

    async Task Because() => _result = await ReadModelComplianceHelper.Apply(
        _complianceManager,
        EventStore,
        EventStoreNamespace,
        _schemaWithoutPii,
        Identifier,
        _instance,
        _converter);

    [Fact] void should_return_original_instance() => _result.ShouldEqual(_instance);
    [Fact] void should_not_call_compliance_manager() => _complianceManager.DidNotReceive().Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
    [Fact] void should_write_subject_to_instance() => ((IDictionary<string, object?>)_result).ContainsKey(WellKnownProperties.Subject).ShouldBeTrue();
    [Fact] void should_write_correct_subject_value() => ((IDictionary<string, object?>)_result)[WellKnownProperties.Subject].ShouldEqual(Identifier);
}
