// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

public class and_the_publication_is_rejected : given.a_sink
{
    IEnumerable<Storage.Sinks.FailedPartition> _failures;

    void Establish() => _destinations.Fail = true;
    async Task Because() => _failures = await _sink.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);

    [Fact] void should_report_a_failed_partition() => _failures.Count().ShouldEqual(1);
    [Fact] void should_name_the_key() => _failures.First().EventSourceId.Value.ShouldEqual("customer-1");
    [Fact] void should_name_the_position() => _failures.First().EventSequenceNumber.Value.ShouldEqual(5UL);
    [Fact] void should_say_why() => _failures.First().Reason.ShouldContain("refused");
}
