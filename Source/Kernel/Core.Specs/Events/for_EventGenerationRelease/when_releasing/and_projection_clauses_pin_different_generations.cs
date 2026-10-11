// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_projection_clauses_pin_different_generations : given.a_release_boundary
{
    readonly EventType _first = new("person-registered", 1);

    void Establish() => _schemas[_first] = new(_first, EventTypeOwner.Client, EventTypeSource.Code, _firstSchema);

    async Task Because() => _result = await _release.Release("store", [_first, _pin], _schemas, [_event]);

    [Fact] void should_deliver_both_clause_generations() => _result.Select(@event => @event.Context.EventType.Generation.Value).ShouldContainOnly(1U, 2U);
    [Fact] void should_release_the_first_shape() => ((IDictionary<string, object?>)_result[0].Content)["name"].ShouldEqual("Ada Lovelace");
    [Fact] void should_release_the_second_shape() => ((IDictionary<string, object?>)_result[1].Content)["fullName"].ShouldEqual("Ada Lovelace");
    [Fact] void should_select_each_generations_hash() => _result.Select(@event => @event.Context.Hash.Value).ShouldContainOnly("first-hash", "second-hash");
}
