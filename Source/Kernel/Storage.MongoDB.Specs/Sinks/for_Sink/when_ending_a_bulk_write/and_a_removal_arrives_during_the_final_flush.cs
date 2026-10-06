// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_a_bulk_write;

public class and_a_removal_arrives_during_the_final_flush : given.a_sink_with_gated_bulk_writes
{
    bool _cachedModelWasRemoved;

    async Task Establish()
    {
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 1);
    }

    async Task Because()
    {
        var ending = _sink.EndBulk();
        await _firstFlushStarted.Task;
        try
        {
            var removal = Changes(0);
            removal.Remove();
            await _sink.ApplyChanges(_firstKey, removal, 2UL);
            _cachedModelWasRemoved = await _sink.FindOrDefault(_firstKey) is null;
        }
        finally
        {
            _releaseFirstFlush.SetResult();
        }

        await ending;
    }

    [Fact] void should_flush_the_removal() => _batches.SelectMany(batch => batch).OfType<DeleteOneModel<BsonDocument>>().Count().ShouldEqual(1);
    [Fact] void should_report_the_cached_model_as_removed() => _cachedModelWasRemoved.ShouldBeTrue();
}
