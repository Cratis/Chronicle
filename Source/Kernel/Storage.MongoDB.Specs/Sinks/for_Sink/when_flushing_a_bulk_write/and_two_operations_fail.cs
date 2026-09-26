// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_two_operations_fail : given.an_ordered_bulk_write
{
    void Establish() => _failureIndexes = [100, 700];
    protected override Key KeyFor(int index) => index switch
    {
        101 => new Key("key-100", ArrayIndexers.NoIndexers),
        701 => new Key("key-700", ArrayIndexers.NoIndexers),
        _ => base.KeyFor(index)
    };
    async Task Because() => await Flush();
    [Fact] void should_skip_later_operations_for_each_failed_partition() => SentExcluding([[], [101], [101, 701]], (0, 1000), (101, 1000), (701, 1000)).ShouldBeTrue();
    [Fact] void should_report_both_failed_partitions() => Failed(100, 700).ShouldBeTrue();
}
