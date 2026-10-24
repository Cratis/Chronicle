// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ShadowColumns.when_migrating_a_composite_key_table;

[Collection(PostgreSqlCollection.Name)]
public class and_the_provider_is_postgresql(PostgreSqlFixture fixture) : given.a_legacy_table
{
    protected override ISqlSinkHarness CreateHarness() => new PostgreSqlSinkHarness { Fixture = fixture };

    Task Because() => MigrateAndCheck();

    [Fact] void should_not_install_a_shadow_for_a_scalar_schema() => _scalarShadows.ShouldEqual(0);
    [Fact] void should_add_one_nullable_default_free_shadow() => _nullableShadows.ShouldEqual(1);
    [Fact] void should_keep_one_index_across_concurrent_and_repeated_migrations() => _indexes.ShouldEqual(1);
    [Fact] void should_adopt_after_a_duplicate_column_error() => (_duplicates > 0).ShouldBeTrue();
    [Fact] void should_not_backfill_ambiguous_legacy_ids() => _unstampedRows.ShouldEqual(1);
    [Fact] void should_use_the_provider_storage_type() => _columnType.ShouldEqual("VARCHAR(850)");
    [Fact] void should_leave_the_shadow_outside_the_ef_model() => _mapped.ShouldBeFalse();
    [Fact] void should_preserve_a_colliding_schema_property() => _collisionValue.ShouldEqual("user property");
    [Fact] void should_warn_about_a_colliding_schema_property() => _collisionWarned.ShouldBeTrue();
    [Fact] void should_refuse_shadow_access_for_a_colliding_schema() => _refused.ShouldBeTrue();
}
