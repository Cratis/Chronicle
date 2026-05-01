// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Compliance.for_ReadModelComplianceHelper.when_releasing;

public class and_schema_has_pii_but_no_subject_in_document : given.all_dependencies
{
    ExpandoObject _result;

    async Task Because() => _result = await ReadModelComplianceHelper.Release(
        _complianceManager,
        EventStore,
        EventStoreNamespace,
        _schemaWithPii,
        _instance,
        _converter);

    [Fact] void should_return_original_instance() => _result.ShouldEqual(_instance);
    [Fact] void should_not_call_compliance_manager() => _complianceManager.DidNotReceive().Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>());
}
