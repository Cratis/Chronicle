// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Statistics.for_StatisticsReader.when_reading_a_row;

public class with_the_key_at_the_top_level : Specification
{
    EventTypeStatistics? _row;

    void Because() => _row = StatisticsReader.ToRow("""{"eventType":"OrderPlaced","namespace":"Default","count":7}""");

    [Fact] void should_read_the_event_type() => _row!.EventType.ShouldEqual("OrderPlaced");
    [Fact] void should_read_the_namespace() => _row!.Namespace.ShouldEqual("Default");
    [Fact] void should_read_the_count() => _row!.Count.ShouldEqual(7);
}
