// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_DurabilityClassifier;

public class when_classifying_durability : Specification
{
    string _sqliteWal = string.Empty;
    string _postgres = string.Empty;
    string _postgresNoFsync = string.Empty;
    string _mongoMajority = string.Empty;

    void Because()
    {
        _sqliteWal = DurabilityClassifier.ForSqlite(1, "wal");
        _postgres = DurabilityClassifier.ForPostgreSql("on", "on");
        _postgresNoFsync = DurabilityClassifier.ForPostgreSql("on", "off");
        _mongoMajority = DurabilityClassifier.ForMongo("majority", null, true);
    }

    [Fact] void should_treat_sqlite_normal_wal_as_deferred() => _sqliteWal.ShouldEqual(DurabilityClassifier.DeferredFlush);
    [Fact] void should_treat_default_postgres_as_sync() => _postgres.ShouldEqual(DurabilityClassifier.SyncOnCommit);
    [Fact] void should_treat_postgres_without_fsync_as_no_flush() => _postgresNoFsync.ShouldEqual(DurabilityClassifier.NoFlush);
    [Fact] void should_treat_journaled_majority_as_sync() => _mongoMajority.ShouldEqual(DurabilityClassifier.SyncOnCommit);
}
