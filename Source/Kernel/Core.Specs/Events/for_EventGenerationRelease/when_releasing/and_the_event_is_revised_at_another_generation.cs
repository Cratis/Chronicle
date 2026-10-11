// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_the_event_is_revised_at_another_generation : given.a_release_boundary
{
    void Establish() => _event = _event with
    {
        Content = _converter.ToExpandoObject(new JsonObject { ["name"] = "revised-name" }, _firstSchema),
        Revisions = [new(1, _event.Context.CorrelationId, [], _event.Context.CausedBy, _event.Context.Occurred)]
    };

    async Task Because() => _result = await _release.Release(_event.Context.EventStore, [_pin], _schemas, [_event]);

    [Fact] void should_migrate_the_revision_not_the_stored_pin() => ((IDictionary<string, object?>)_result[0].Content)["fullName"].ShouldEqual("revised-name");
    [Fact] void should_not_claim_a_durable_hash() => _result[0].Context.Hash.ShouldEqual(EventHash.NotSet);
}
