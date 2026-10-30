// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_CompositeKeyEncoding.when_encoding;

public class and_date_times_are_given_in_other_offsets : Specification
{
    string _offset;
    string _utc;
    string _utcDateTime;
    string _unspecified;

    void Because()
    {
        _offset = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = new DateTimeOffset(2026, 10, 23, 10, 0, 0, TimeSpan.FromHours(2)) });
        _utc = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = new DateTimeOffset(2026, 10, 23, 8, 0, 0, TimeSpan.Zero) });
        _utcDateTime = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = new DateTime(2026, 10, 23, 8, 0, 0, DateTimeKind.Utc) });
        _unspecified = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = new DateTime(2026, 10, 23, 8, 0, 0, DateTimeKind.Unspecified) });
    }

    [Fact] void should_normalize_the_offset_to_utc() => _offset.ShouldEqual(_utc);
    [Fact] void should_use_the_round_trip_format() => _utc.ShouldEqual("~k1:33:2026-10-23T08:00:00.0000000+00:00");
    [Fact] void should_write_a_utc_date_time_with_a_z() => _utcDateTime.ShouldEqual("~k1:28:2026-10-23T08:00:00.0000000Z");
    [Fact] void should_treat_an_unspecified_date_time_as_utc() => _unspecified.ShouldEqual(_utcDateTime);
}
