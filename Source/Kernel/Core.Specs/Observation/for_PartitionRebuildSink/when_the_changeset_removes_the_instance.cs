// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildSink;

public class when_the_changeset_removes_the_instance : given.a_partition_rebuild_sink
{
    ExpandoObject? _result;

    async Task Because()
    {
        await _sink.ApplyChanges(_partition, ChangesetSetting("name", "first"), 5);
        await _sink.ApplyChanges(_partition, ChangesetRemoving(_sink.State!), 6);
        _result = await _sink.FindOrDefault(_partition);
    }

    [Fact] void should_find_nothing() => _result.ShouldBeNull();
    [Fact] void should_be_removed() => _sink.IsRemoved.ShouldBeTrue();
    [Fact] void should_track_the_watermark() => _sink.Watermark.ShouldEqual((EventSequenceNumber)6);
}
