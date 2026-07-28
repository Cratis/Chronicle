// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_guarded_on_watermark;

/// <summary>
/// The regression test for the reason the guard is scoped: a projection that collapses several event sources onto
/// one document is written out of order on purpose, and an unguarded write must still land behind the watermark.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
[Collection(MongoDBCollection.Name)]
public class and_an_out_of_order_event_is_applied_unguarded(MongoDBFixture fixture) : given.an_accumulating_read_model(fixture)
{
    int _count;

    async Task Establish() =>
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);

    async Task Because()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 7UL);
        _count = await CurrentCount();
    }

    [Fact] void should_apply_the_out_of_order_event() => _count.ShouldEqual(2);
}
