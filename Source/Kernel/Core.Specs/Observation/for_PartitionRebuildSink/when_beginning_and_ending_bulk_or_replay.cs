// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildSink;

public class when_beginning_and_ending_bulk_or_replay : given.a_partition_rebuild_sink
{
    Exception _error;

    async Task Because()
    {
        var replay = new ReplayContext(new("model", 1), "replay", "revert", DateTimeOffset.UtcNow);
        _error = await Catch.Exception(async () =>
        {
            await _sink.BeginBulk();
            await _sink.EndBulk();
            await _sink.BeginReplay(replay);
            await _sink.ResumeReplay(replay);
            await _sink.EndReplay(replay);
        });
    }

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_not_support_isolated_replace() => ((ISink)_sink).SupportsIsolatedReplace.ShouldBeFalse();
}
