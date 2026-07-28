// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_guarded_on_watermark;

/// <summary>
/// Bulk mode answers reads from the pending-state cache, so a guarded write the server would reject must not be
/// allowed to seed that cache: the next event for the same key would read the doubled state and build on it. The
/// read after the rejected write is what makes this bite — a scenario that only checks the stored document at the
/// end exercises the server-side filter alone and says nothing about the in-memory watermark the sink keeps for
/// the window.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class and_a_read_follows_a_rejected_write_inside_a_bulk_window(MongoDBFixture fixture) : given.an_accumulating_read_model(fixture)
{
    int _countReadAfterTheRejectedWrite;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);
        await _sink.BeginBulk();
    }

    async Task Because()
    {
        // Seeds the window's watermark from the stored document, exactly as SetInitialState does.
        await _sink.FindOrDefault(_key);

        // Already applied at 42: must neither reach the server nor be left in the pending-state cache.
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(99), 42UL, SinkWriteMode.OnlyWhenAdvancingWatermark);

        var instance = await _sink.FindOrDefault(_key);
        _countReadAfterTheRejectedWrite = Convert.ToInt32(
            ((IDictionary<string, object?>)instance!)["count"],
            CultureInfo.InvariantCulture);

        await _sink.EndBulk();
    }

    [Fact] void should_not_hand_the_rejected_state_to_the_next_event() => _countReadAfterTheRejectedWrite.ShouldEqual(1);
}
