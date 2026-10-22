// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

[Collection(SqlServerCollection.Name)]
public class and_extended_indexes_are_reconciled_on_sql_server(SqlServerFixture fixture) : given.an_extended_index_sink
{
    protected override ISqlSinkHarness CreateHarness() => new SqlServerSinkHarness { Fixture = fixture, Logger = _logger };

    Task Because() => VerifyIndexes();

    [Fact] void should_create_nested_and_supported_array_indexes() => VerifyCreateNestedAndSupportedArrayIndexes();
    [Fact] void should_use_the_nested_index_for_a_lookup() => VerifyUseTheNestedIndexForALookup();
    [Fact] void should_remove_undeclared_expression_and_gin_indexes() => VerifyRemoveUndeclaredExpressionAndGinIndexes();
    [Fact] void should_create_both_ordered_replacements() => VerifyCreateBothOrderedReplacements();
    [Fact] void should_store_both_descending_keys() => VerifyStoreBothDescendingKeys();
    [Fact] void should_remove_an_orphaned_owned_computed_column() => VerifyRemoveAnOrphanedOwnedComputedColumn();
    [Fact] void should_preserve_user_indexes() => VerifyPreserveUserIndexes();
    [Fact] void should_keep_computed_columns_non_persisted() => VerifyKeepComputedColumnsNonPersisted();
    [Fact] void should_preserve_primary_data() => VerifyPreservePrimaryData();
    [Fact] void should_report_unsupported_array_paths() => VerifyReportUnsupportedArrayPaths();
    [Fact] void should_not_hide_index_creation_failures() => VerifyNotHideIndexCreationFailures();
}
