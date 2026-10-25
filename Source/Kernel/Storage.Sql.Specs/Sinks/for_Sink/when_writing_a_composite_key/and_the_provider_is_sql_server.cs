// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_a_composite_key;

[Collection(SqlServerCollection.Name)]
public class and_the_provider_is_sql_server(SqlServerFixture fixture) : given.a_composite_key_sink
{
    protected override ISqlSinkHarness CreateHarness() => new SqlServerSinkHarness { Fixture = fixture };

    Task Because() => WriteAndInspect();

    [Fact] void should_preserve_the_legacy_primary_key() => _rows.Single().Id.ShouldEqual("a_b");
    [Fact] void should_stamp_the_canonical_encoding() => _rows.Single().Stamp.ShouldEqual("~k1:1:a1:b");
    [Fact] void should_preserve_subsequent_writes() => _rows.Single().Name.ShouldEqual("second");
    [Fact] void should_add_one_nullable_default_free_shadow() => _shadowColumns.ShouldEqual(1);
    [Fact] void should_add_one_shadow_index() => _shadowIndexes.ShouldEqual(1);
}
