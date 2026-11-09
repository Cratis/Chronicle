// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_event_sources_that_differ_only_in_spelling;

public class on_sqlite : conformance
{
    given.EventSequenceStorageHost? _host;

    void Destroy() => _host?.Dispose();

    protected override Task<IEventSequenceStorage> OpenStorage() =>
        Task.FromResult((_host ??= given.EventSequenceStorageHost.ForSqlite(Identities, EventTypes)).Open());
}
