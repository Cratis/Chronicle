// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildSink;

public class when_applying_changes_for_the_partition : given.a_partition_rebuild_sink
{
    ExpandoObject? _result;

    async Task Because()
    {
        await _sink.ApplyChanges(_partition, ChangesetSetting("name", "first"), 5);
        await _sink.ApplyChanges(_partition, ChangesetSetting("name", "second", _sink.State), 9);
        _result = await _sink.FindOrDefault(_partition);
    }

    [Fact] void should_hold_the_resulting_state() => ((IDictionary<string, object?>)_result!)["name"].ShouldEqual("second");
    [Fact] void should_track_the_watermark() => _sink.Watermark.ShouldEqual((EventSequenceNumber)9);
    [Fact] void should_not_be_removed() => _sink.IsRemoved.ShouldBeFalse();
}
