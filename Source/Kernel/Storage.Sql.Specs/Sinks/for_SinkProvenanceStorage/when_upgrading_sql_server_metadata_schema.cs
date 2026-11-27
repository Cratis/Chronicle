// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage;

public class when_upgrading_sql_server_metadata_schema : given.a_schema_upgrade
{
    Task Because() => Exercise(sqlServer: true);

    [Fact] void should_allow_a_legacy_table_missing_with_error_208() => _missingClean.ShouldBeTrue();
    [Fact] void should_not_require_catalog_visibility_to_read_a_legacy_marker() => _readStatements.Any(sql => sql.Contains("OBJECT_ID", StringComparison.Ordinal) || sql.Contains("HAS_PERMS_BY_NAME", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_create_or_migrate_metadata_during_reads() => _readStatements.All(sql => sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    [Fact] void should_propagate_permission_error_229() => ((SqlException)_error!).Number.ShouldEqual(229);
    [Fact] void should_propagate_conversion_error_245() => ((SqlException)_providerCorruptError!).Number.ShouldEqual(245);
    [Fact] void should_allow_an_established_absent_legacy_marker() => _initiallyClean.ShouldBeTrue();
    [Fact] void should_refuse_malformed_legacy_metadata() => _corruptError.ShouldNotBeNull();
    [Fact] void should_fail_closed_if_legacy_metadata_cannot_be_read() => _error.ShouldNotBeNull();
    [Fact] void should_preserve_the_default_schema_restriction() => _restricted.ShouldBeTrue();
    [Fact] void should_import_the_marker_before_the_old_marker_disappears() => _retained.ShouldBeTrue();
    [Fact] void should_query_the_default_schema() => (_commands.LegacyReads > 0).ShouldBeTrue();
}
