// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Specifications;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_SqlProviderHost;

public class when_started_for_sqlite : Specification
{
    SqlProviderHost _host = default!;
    Type _connectionType = default!;
    CommandSnapshot _snapshot = default!;

    async Task Because()
    {
        _host = new(StorageProvider.Sqlite);
        await _host.Start();
        _connectionType = _host.ConnectionType();
        var storage = _host.CreateSequence();
        await storage.GetTailSequenceNumber();
        _snapshot = _host.Counter.Snapshot();
    }

    async Task Destroy() => await _host.DisposeAsync();

    [Fact] void should_use_the_providers_own_connection_type() => _connectionType.ShouldEqual(typeof(SqliteConnection));
    [Fact] void should_count_the_commands_it_issued() => (_snapshot.Total > 0).ShouldBeTrue();
}
