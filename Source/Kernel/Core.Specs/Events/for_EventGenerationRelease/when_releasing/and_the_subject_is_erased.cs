// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_the_subject_is_erased : given.a_release_boundary
{
    void Establish()
    {
        _event = _event with { GenerationalContent = new Dictionary<int, string> { [1] = "{\"name\":\"ciphertext\"}" } };
        _metadata.ReleaseStrict(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(new JsonObject { ["name"] = string.Empty });
    }

    async Task Because() => _result = await _release.Release(_event.Context.EventStore, [_pin], _schemas, [_event]);

    [Fact] void should_migrate_erasure_without_recovering_personal_data() => ((IDictionary<string, object?>)_result[0].Content)["fullName"].ShouldEqual(string.Empty);
}
