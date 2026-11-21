// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_KnownGaps;

public class when_asking_about_the_activation_state_read : Specification
{
    string? _sqlite;
    string? _postgres;
    string? _sqlServer;
    string? _mongo;
    string? _otherBenchmark;
    string _summary = default!;

    void Because()
    {
        _sqlite = KnownGaps.ReasonFor(StorageProvider.Sqlite, "GetState");
        _postgres = KnownGaps.ReasonFor(StorageProvider.PostgreSql, "GetState");
        _sqlServer = KnownGaps.ReasonFor(StorageProvider.SqlServer, "GetState");
        _mongo = KnownGaps.ReasonFor(StorageProvider.Mongo, "GetState");
        _otherBenchmark = KnownGaps.ReasonFor(StorageProvider.Sqlite, "GetTail");
        _summary = RunSummary.ForProvider("sqlite", KnownGaps.For(StorageProvider.Sqlite));
    }

    [Fact] void should_measure_it_on_sqlite() => _sqlite.ShouldBeNull();
    [Fact] void should_measure_it_on_postgres() => _postgres.ShouldBeNull();
    [Fact] void should_measure_it_on_sql_server() => _sqlServer.ShouldBeNull();
    [Fact] void should_measure_it_on_mongo() => _mongo.ShouldBeNull();
    [Fact] void should_measure_other_benchmarks_on_sql() => _otherBenchmark.ShouldBeNull();
    [Fact] void should_not_list_it_as_not_measured_in_the_summary() => _summary.ShouldNotContain("| sqlite | activation-get-state | — | — | — | not measured");
    [Fact] void should_have_no_gaps_on_any_provider() => Enum.GetValues<StorageProvider>().All(provider => KnownGaps.For(provider).Count == 0).ShouldBeTrue();
}
