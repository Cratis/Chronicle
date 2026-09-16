// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_StoredTimestamps;

public class when_normalizing_submillisecond_metadata : Specification
{
    readonly DateTimeOffset _input = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(2)).AddTicks(1234567);
    DateTimeOffset _result;
    Causation _cause;
    Causation _normalizedCause;

    void Establish() => _cause = new(_input, "cause", new Dictionary<string, string> { ["source"] = "caller" });

    void Because()
    {
        _result = StoredTimestamps.Normalize(_input);
        _normalizedCause = StoredTimestamps.Normalize(_cause);
    }

    [Fact] void should_match_the_bson_datetime_roundtrip() => _result.UtcDateTime.ShouldEqual(new BsonDateTime(_input.UtcDateTime).ToUniversalTime());
    [Fact] void should_report_the_stored_utc_offset() => _result.Offset.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_match_causation_timestamp_precision() => _normalizedCause.Occurred.ShouldEqual(_result);
    [Fact] void should_preserve_causation_details() => _normalizedCause.Properties.ShouldEqual(_cause.Properties);
    [Fact] void should_not_mutate_the_original_causation() => _cause.Occurred.ShouldEqual(_input);
}
