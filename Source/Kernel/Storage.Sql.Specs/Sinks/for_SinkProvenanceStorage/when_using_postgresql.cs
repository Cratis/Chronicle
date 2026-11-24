// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage;

[Collection(PostgreSqlCollection.Name)]
public class when_using_postgresql(PostgreSqlFixture fixture) : given.a_provenance_database
{
    string _primary;
    string _independentDatabase;

    async Task Because()
    {
        _primary = await fixture.CreateDatabase();
        _independentDatabase = await fixture.CreateDatabase();
        await Execute(_primary, "CREATE SCHEMA alternate");
        var alias = new NpgsqlConnectionStringBuilder(_primary) { SearchPath = "alternate" };
        await Exercise(_primary, _independentDatabase, alias.ConnectionString);
    }

    [Fact] void should_allow_a_database_without_positive_evidence() => _empty.ShouldBeFalse();
    [Fact] void should_share_a_monotonic_mark_across_resolved_database_aliases() => _alias.ShouldBeTrue();
    [Fact] void should_retain_the_mark_after_context_and_coordinator_recreation() => _reloaded.ShouldBeTrue();
    [Fact] void should_read_existing_metadata_in_verify_mode() => _verify.ShouldBeTrue();
    [Fact] void should_leave_an_independent_physical_database_unmarked() => _independent.ShouldBeFalse();
    [Fact] void should_fail_closed_on_an_unsupported_marker_version() => _malformedError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_fail_closed_on_missing_metadata_in_verify_mode() => _missingError.ShouldNotBeNull();
    [Fact] void should_only_open_the_reserved_metadata_table() => _openedTables.TrueForAll(table => table == global::Cratis.Chronicle.Storage.Sinks.SinkProvenanceNames.Metadata).ShouldBeTrue();

    async Task Destroy()
    {
        await fixture.DropDatabase(_primary);
        await fixture.DropDatabase(_independentDatabase);
    }
}
