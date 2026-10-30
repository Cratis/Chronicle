// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_bulk;

public class and_a_replay_window_was_open : given.a_sink_with_gated_bulk_writes
{
    ReplayContext _context;

    async Task Establish()
    {
        _holdFirstFlush = false;
        _context = new ReplayContext(new ReadModelType("bulk-model", ReadModelGeneration.First), "bulk", "revert-bulk", DateTimeOffset.UtcNow);
        await _sink.ResumeReplay(_context);
        await _sink.EndBulk();
        await _collections.ResumeReplay(_context);
    }

    Task Because() => _sink.ResumeBulk();

    [Fact] void should_not_treat_resuming_as_leaving_a_replay() => _collections.GetCollection().CollectionNamespace.CollectionName.ShouldEqual("replay-BulkModel");
}
