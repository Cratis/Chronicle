// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_later_operations_target_the_same_key : given.an_ordered_bulk_write
{
    void Establish() => _failureIndexes = [100];
    protected override Key KeyFor(int index) => index == 101 ? new Key("key-100", ArrayIndexers.NoIndexers) : base.KeyFor(index);
    async Task Because() => await Flush();
    [Fact] void should_preserve_the_order_of_operations_for_that_key() => Sent((0, 1000), (101, 1000)).ShouldBeTrue();
    [Fact] void should_report_the_failed_partition_once() => Failed(100).ShouldBeTrue();
}
