// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_the_context_generation_is_the_pinned_one : given.a_release_boundary
{
    void Establish() => _event = _event with
    {
        Context = _event.Context with { EventType = _pin, Hash = "current-hash" },
        Content = _converter.ToExpandoObject(new JsonObject { ["fullName"] = "current-name" }, _schema)
    };

    async Task Because() => _result = await _release.Release(_event.Context.EventStore, [_pin], _schemas, [_event]);

    [Fact] void should_keep_current_content() => ((IDictionary<string, object?>)_result[0].Content)["fullName"].ShouldEqual("current-name");
    [Fact] void should_keep_current_hash() => _result[0].Context.Hash.Value.ShouldEqual("current-hash");
}
