// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage;

[Collection(SqlServerCollection.Name)]
public class when_using_sql_server(SqlServerFixture fixture) : given.a_provenance_database
{
    string _primary;
    string _independentDatabase;
    Exception? _catalogEquivalentError;

    async Task Because()
    {
        _primary = await fixture.CreateDatabase();
        _independentDatabase = await fixture.CreateDatabase();
        var alias = new SqlConnectionStringBuilder(_primary) { ApplicationName = "provenance-alias" };
        await Exercise(_primary, _independentDatabase, alias.ConnectionString);
        var reserved = new SinkProvenanceStorage(DatabaseFor(_primary), "store", "namespace", "__cratis_sink_provenance ");
        _catalogEquivalentError = await Catch.Exception(reserved.IsQuarantined);
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
    [Fact] void should_only_open_the_reserved_metadata_table() => _openedTables.TrueForAll(table => string.Equals(table, global::Cratis.Chronicle.Storage.Sinks.SinkProvenanceNames.Metadata, StringComparison.Ordinal) || string.Equals(table, global::Cratis.Chronicle.Storage.Sinks.SinkProvenanceNames.Inventory, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reserve_catalog_equivalent_names_not_just_ordinal_matches() => _catalogEquivalentError.ShouldBeOfExactType<InvalidOperationException>();

    async Task Destroy()
    {
        await fixture.DropDatabase(_primary);
        await fixture.DropDatabase(_independentDatabase);
    }
}
