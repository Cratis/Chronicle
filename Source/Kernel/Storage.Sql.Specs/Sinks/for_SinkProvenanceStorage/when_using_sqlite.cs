// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage;

public class when_using_sqlite : given.a_provenance_database
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), $"cratis-provenance-{Guid.NewGuid():N}");
    Exception? _writeError;
    bool _markedAfterFailure;

    async Task Because()
    {
        Directory.CreateDirectory(_directory);
        var connection = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, "primary.db") };
        var independent = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, "independent.db") };
        var alias = new SqliteConnectionStringBuilder(connection.ConnectionString) { Pooling = false };
        await Exercise(connection.ConnectionString, independent.ConnectionString, alias.ConnectionString);
        var unmarked = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, "unmarked.db") };
        await new SinkProvenanceStorage(DatabaseFor(unmarked.ConnectionString), "store", "namespace").IsQuarantined();
        var readOnly = new SqliteConnectionStringBuilder(unmarked.ConnectionString) { Mode = SqliteOpenMode.ReadOnly, Pooling = false };
        _writeError = await Catch.Exception(() => new SinkProvenanceStorage(DatabaseFor(readOnly.ConnectionString, SchemaMode.Verify), "store", "namespace").MarkQuarantined());
        _markedAfterFailure = await new SinkProvenanceStorage(DatabaseFor(unmarked.ConnectionString, SchemaMode.Verify), "store", "namespace").IsQuarantined();
    }

    [Fact] void should_share_the_durable_identity_across_aliases() => _stableIdentity.ShouldBeTrue();
    [Fact] void should_discard_late_inventory_evidence() => _lateEvidenceDiscarded.ShouldBeTrue();
    [Fact] void should_commit_inventory_mark_and_completion_together() => _atomicRestrictedCompletion.ShouldBeTrue();
    [Fact] void should_allow_a_database_without_positive_evidence() => _empty.ShouldBeFalse();
    [Fact] void should_share_a_monotonic_mark_across_resolved_database_aliases() => _alias.ShouldBeTrue();
    [Fact] void should_retain_the_mark_after_context_and_coordinator_recreation() => _reloaded.ShouldBeTrue();
    [Fact] void should_read_existing_metadata_in_verify_mode() => _verify.ShouldBeTrue();
    [Fact] void should_leave_an_independent_physical_database_unmarked() => _independent.ShouldBeFalse();
    [Fact] void should_fail_closed_on_an_unsupported_marker_version() => _malformedError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_fail_closed_on_missing_metadata_in_verify_mode() => _missingError.ShouldNotBeNull();
    [Fact] void should_only_open_the_reserved_metadata_table() => _openedTables.TrueForAll(table => table is global::Cratis.Chronicle.Storage.Sinks.SinkProvenanceNames.Metadata or global::Cratis.Chronicle.Storage.Sinks.SinkProvenanceNames.Inventory).ShouldBeTrue();
    [Fact] void should_fail_closed_when_the_durable_mark_cannot_be_written() => _writeError.ShouldNotBeNull();
    [Fact] void should_not_report_a_marker_that_was_never_persisted() => _markedAfterFailure.ShouldBeFalse();

    void Destroy()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, true);
    }
}
