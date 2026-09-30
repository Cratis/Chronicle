// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Statistics.for_StatisticsReader.when_reading_a_row;

public class with_the_key_in_the_document_identifier : Specification
{
    EventTypeStatistics? _row;

    void Because() => _row = StatisticsReader.ToRow("""{"_id":{"EventType":"OrderPlaced","Namespace":"Tenant2"},"Count":42}""");

    [Fact] void should_read_the_event_type_from_the_identifier() => _row!.EventType.ShouldEqual("OrderPlaced");
    [Fact] void should_read_the_namespace_from_the_identifier() => _row!.Namespace.ShouldEqual("Tenant2");
    [Fact] void should_read_the_count() => _row!.Count.ShouldEqual(42);
}
