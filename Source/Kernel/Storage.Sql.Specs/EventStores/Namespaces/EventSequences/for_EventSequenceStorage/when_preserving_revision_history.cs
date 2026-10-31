// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_preserving_revision_history : EventRevisionContract, IDisposable
{
    given.EventSequenceStorageHost? _host;

    public void Dispose()
    {
        _host?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override Task<IEventSequenceStorage> OpenStorage() =>
        Task.FromResult((_host ??= given.EventSequenceStorageHost.ForSqlite(Identities, EventTypes)).Open());
}
