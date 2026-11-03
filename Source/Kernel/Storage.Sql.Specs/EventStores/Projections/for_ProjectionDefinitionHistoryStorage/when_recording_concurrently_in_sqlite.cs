// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Projections.for_ProjectionDefinitionHistoryStorage;

public class when_recording_concurrently_in_sqlite : given.a_sqlite_history_storage
{
    async Task Because()
    {
        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => _storage.Record(_entry)));
        await Read();
    }
    [Fact] void should_keep_one_entry() => _entries.Length.ShouldEqual(1);
}
