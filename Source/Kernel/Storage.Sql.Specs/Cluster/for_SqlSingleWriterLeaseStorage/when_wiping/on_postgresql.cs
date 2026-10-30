// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Storage.SingleWriter;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.when_wiping;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql(PostgreSqlFixture fixture) : for_Database.given.a_postgresql_database(fixture)
{
    readonly SingleWriterLeaseClaim _claim = new(new("Owner-A"), TimeSpan.FromSeconds(30), "cluster", "service", "host", "1.0");
    SqlSingleWriterLeaseStorage _storage;
    bool _renewed;
    SingleWriterLeaseAttempt _other;

    async Task Establish()
    {
        _storage = new(Database);
        await _storage.TryAcquire(_claim, CancellationToken.None);
    }

    async Task Because()
    {
        await Database.Wipe();
        _renewed = await _storage.Renew(_claim.Owner, _claim.Ttl, CancellationToken.None);
        _other = await _storage.TryAcquire(_claim with { Owner = new("Owner-B") }, CancellationToken.None);
    }

    [Fact] void should_keep_the_lease_for_its_owner() => _renewed.ShouldBeTrue();
    [Fact] void should_refuse_another_owner() => _other.Acquired.ShouldBeFalse();
}
#endif
