// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_a_write_concern_error_occurs_for_repeated_partition : given.an_ordered_bulk_write
{
    void Establish() => _writeConcernFailure = true;
    protected override Key KeyFor(int index) => index is 101 or 700 ? new Key("key-100", ArrayIndexers.NoIndexers) : base.KeyFor(index);
    async Task Because() => await Flush();
    [Fact] void should_not_resend_operations_with_an_unknown_outcome() => Sent((0, 1000)).ShouldBeTrue();
    [Fact] void should_report_each_partition_once_at_its_earliest_sequence_number() => Failed(Enumerable.Range(0, OperationCount).Except([101, 700]).ToArray()).ShouldBeTrue();
}
