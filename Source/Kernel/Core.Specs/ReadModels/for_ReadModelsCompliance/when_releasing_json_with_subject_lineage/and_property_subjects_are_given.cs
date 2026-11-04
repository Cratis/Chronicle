// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_releasing_json_with_subject_lineage;

public class and_property_subjects_are_given : given.all_dependencies
{
    readonly Dictionary<string, string[]> _releasedProperties = [];

    void Establish() => _complianceManager.Release(
            Arg.Any<EventStoreName>(),
            Arg.Any<EventStoreNamespaceName>(),
            Arg.Any<JsonSchema>(),
            Arg.Any<string>(),
            Arg.Any<JsonObject>())
        .Returns(callInfo =>
        {
            var slice = callInfo.ArgAt<JsonObject>(4);
            _releasedProperties[callInfo.ArgAt<string>(3)] = slice.Select(_ => _.Key).ToArray();
            return Task.FromResult(slice);
        });

    async Task Because() => await _compliance.ReleaseJson(
        EventStore,
        EventStoreNamespace,
        _schemaWithPii,
        new JsonObject { ["name"] = "encrypted-name", ["value"] = "other" },
        "subject-1",
        new Dictionary<string, string> { ["name"] = "subject-2" });

    [Fact] void should_release_the_property_under_its_own_subject() => _releasedProperties["subject-2"].ShouldEqual(["name"]);
    [Fact] void should_release_the_rest_under_the_default_subject() => _releasedProperties["subject-1"].ShouldEqual(["value"]);
}
