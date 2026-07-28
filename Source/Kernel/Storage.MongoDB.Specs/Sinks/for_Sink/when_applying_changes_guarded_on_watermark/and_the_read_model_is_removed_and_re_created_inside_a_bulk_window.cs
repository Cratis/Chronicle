// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_guarded_on_watermark;

/// <summary>
/// A delete queued in an open bulk window has not reached the server, so an uncached read would still find the
/// document and the caller would treat the next event as an update of a live instance. Because a guarded write
/// never inserts, the queued delete would then run first and the re-creating update would match nothing — losing
/// the read model outright, where the previous upserting write kept it.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_read_model_is_removed_and_re_created_inside_a_bulk_window(MongoDBFixture fixture) : given.an_accumulating_read_model(fixture)
{
    ExpandoObject? _result;
    ExpandoObject? _readDuringTheWindow;
    int _count;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);
        await _sink.BeginBulk();
    }

    async Task Because()
    {
        await _sink.ApplyChanges(_key, RemovalChangeset(), 43UL, SinkWriteMode.OnlyWhenAdvancingWatermark);

        // This is the read SetInitialState performs, and the answer it gets decides whether the pipeline treats
        // the next event as creating the instance.
        _readDuringTheWindow = await _sink.FindOrDefault(_key);

        // Mirrors SaveChanges.WriteModeFor: an instance the sink reported as present is written with the guard,
        // and a guarded write never inserts. A read that lies about a queued delete therefore does not merely
        // return stale state — it downgrades the re-creating write into one that cannot re-create anything.
        var mode = _readDuringTheWindow is null
            ? SinkWriteMode.Always
            : SinkWriteMode.OnlyWhenAdvancingWatermark;

        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(7), 44UL, mode);
        await _sink.EndBulk();
        _result = await _sink.FindOrDefault(_key);
        if (_result is not null)
        {
            _count = await CurrentCount();
        }
    }

    [Fact] void should_report_the_removed_read_model_as_gone_while_the_delete_is_queued() => _readDuringTheWindow.ShouldBeNull();
    [Fact] void should_re_create_the_read_model() => _result.ShouldNotBeNull();
    [Fact] void should_hold_the_re_created_state() => _count.ShouldEqual(7);
}
