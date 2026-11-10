// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using MongoDB.Bson;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_KernelObserverInspection;

/// <summary>
/// Specifies that observer definitions cannot masquerade as persisted drain state.
/// </summary>
public class when_summarizing_documents : Specification
{
    (int Documents, int InFlight) _result;

    void Because() => _result = KernelObserverInspection.Summarize(
    [
        new() { ["_id"] = "observer", ["eventTypes"] = new BsonDocument() },
        new() { ["_id"] = "observer", ["inFlightPartitions"] = new BsonArray { "source" } }
    ]);

    [Fact] void should_count_only_state_documents() => _result.Documents.ShouldEqual(1);
    [Fact] void should_count_in_flight_partitions() => _result.InFlight.ShouldEqual(1);
    [Fact] void should_not_count_a_definition_as_drained_state() => KernelObserverInspection.Summarize([new() { ["eventTypes"] = new BsonDocument() }]).Documents.ShouldEqual(0);
    [Fact] void should_reject_state_without_an_in_flight_array() => Assert.Throws<InvalidDataException>(() => KernelObserverInspection.Summarize([new() { ["_id"] = "observer" }]));
    [Fact] void should_reject_a_malformed_in_flight_array() => Assert.Throws<InvalidDataException>(() => KernelObserverInspection.Summarize([new() { ["inFlightPartitions"] = "invalid" }]));
}
