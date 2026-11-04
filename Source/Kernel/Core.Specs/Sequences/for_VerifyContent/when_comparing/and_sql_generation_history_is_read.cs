// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_sql_generation_history_is_read : given.a_sql_event
{
    StoredEventGenerations _history;
    readonly GenerationProvenance _provenance = new(1, true, new("recorded-version"));

    async Task Establish()
    {
        var observed = (await _sql.GetStoredGenerations(EventSequenceNumber.First))!;
        var schema = (await _storage.GetEventStore("store").EventTypes.GetFor("event", 1U)).Schema;
        var content = _converter.ToExpandoObject(JsonNode.Parse("""{"value":42}""")!.AsObject(), schema);
        (await _sql.TryAddGenerations(observed, [new(3, content, EventHash.NotSet, _provenance)])).ShouldBeTrue();
    }

    async Task Because() => _history = (await _sql.GetStoredGenerations(EventSequenceNumber.First))!;

    [Fact] void should_expose_the_recorded_provenance() => _history.DerivedGenerations[new(3)].ShouldEqual(_provenance);
}
