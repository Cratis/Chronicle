// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_releasing;

public class and_schema_has_pii_but_no_subject_in_document : given.all_dependencies
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _compliance.Release(
        EventStore,
        EventStoreNamespace,
        _schemaWithPii,
        _instance));

    [Fact] void should_not_refuse_the_read() => _error.ShouldBeNull();
    [Fact] void should_release_with_unattributed_subject_status() => _complianceManager.Received(1).Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), string.Empty, Arg.Any<JsonObject>());
}
