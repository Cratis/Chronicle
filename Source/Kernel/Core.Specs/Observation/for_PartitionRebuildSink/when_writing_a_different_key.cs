// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildSink;

public class when_writing_a_different_key : given.a_partition_rebuild_sink
{
    Exception _writeError;
    Exception _readError;

    async Task Because()
    {
        var other = new Key("other", ArrayIndexers.NoIndexers);
        _writeError = await Catch.Exception(() => _sink.ApplyChanges(other, ChangesetSetting("name", "x"), 1));
        _readError = await Catch.Exception(() => _sink.FindOrDefault(other));
    }

    [Fact] void should_refuse_the_write() => _writeError.ShouldBeOfExactType<PartitionRebuildEscapedPartition>();
    [Fact] void should_refuse_the_read() => _readError.ShouldBeOfExactType<PartitionRebuildEscapedPartition>();
    [Fact] void should_not_hold_any_state() => _sink.State.ShouldBeNull();
}
