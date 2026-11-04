// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_releasing_json_with_subject_lineage;

public class and_a_subject_is_given : given.all_dependencies
{
    JsonObject _result;
    string _releasedUnder;

    void Establish() => _complianceManager.Release(
            Arg.Any<EventStoreName>(),
            Arg.Any<EventStoreNamespaceName>(),
            Arg.Any<JsonSchema>(),
            Arg.Any<string>(),
            Arg.Any<JsonObject>())
        .Returns(callInfo =>
        {
            _releasedUnder = callInfo.ArgAt<string>(3);
            return Task.FromResult(new JsonObject { ["name"] = "decrypted-name" });
        });

    async Task Because() => _result = await _compliance.ReleaseJson(
        EventStore,
        EventStoreNamespace,
        _schemaWithPii,
        new JsonObject { ["name"] = "encrypted-name" },
        "subject-1",
        new Dictionary<string, string>());

    [Fact] void should_release_under_the_given_subject() => _releasedUnder.ShouldEqual("subject-1");
    [Fact] void should_return_the_released_document() => _result["name"].GetValue<string>().ShouldEqual("decrypted-name");
}
