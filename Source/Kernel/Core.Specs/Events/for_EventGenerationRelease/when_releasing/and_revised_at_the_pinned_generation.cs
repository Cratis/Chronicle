// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_revised_at_the_pinned_generation : given.a_release_boundary
{
    void Establish() => _event = _event with
    {
        Context = _event.Context with { EventType = _pin, Hash = "revision-hash" },
        Content = _converter.ToExpandoObject(new JsonObject { ["fullName"] = "revised-name" }, _schema),
        Revisions = [new(2, _event.Context.CorrelationId, [], _event.Context.CausedBy, _event.Context.Occurred)]
    };

    async Task Because() => _result = await _release.Release(_event.Context.EventStore, [_pin], _schemas, [_event]);

    [Fact] void should_deliver_the_revision() => ((IDictionary<string, object?>)_result[0].Content)["fullName"].ShouldEqual("revised-name");
    [Fact] void should_keep_the_revision_hash() => _result[0].Context.Hash.Value.ShouldEqual("revision-hash");
}
