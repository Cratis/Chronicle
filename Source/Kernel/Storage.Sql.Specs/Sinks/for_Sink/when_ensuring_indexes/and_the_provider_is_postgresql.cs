// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

[Collection(PostgreSqlCollection.Name)]
public class and_the_provider_is_postgresql(PostgreSqlFixture fixture) : given.an_indexed_sink
{
    protected override ISqlSinkHarness CreateHarness() => new PostgreSqlSinkHarness { Fixture = fixture };

    Task Because() => EnsureAndReplay();

    [Fact] void should_create_only_supported_scalar_indexes() => _indexedColumns.ShouldEqual(["name", "number]quote"]);
    [Fact] void should_create_indexes_on_the_existing_table() => _initialCount.ShouldEqual(2);
    [Fact] void should_be_idempotent() => _repeatedCount.ShouldEqual(2);
    [Fact] void should_index_the_shadow() => _shadowCount.ShouldEqual(2);
    [Fact] void should_preserve_indexes_on_promotion() => _promotedCount.ShouldEqual(2);
    [Fact] void should_allow_a_second_indexed_shadow() => _secondShadowCount.ShouldEqual(2);
    [Fact] void should_preserve_indexes_on_the_second_promotion() => _secondPromotedCount.ShouldEqual(2);
    [Fact] void should_respect_identifier_byte_limit() => _names.All(name => System.Text.Encoding.UTF8.GetByteCount(name) <= 63).ShouldBeTrue();
}
