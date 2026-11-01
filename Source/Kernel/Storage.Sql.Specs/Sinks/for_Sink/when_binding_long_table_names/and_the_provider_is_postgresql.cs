// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_binding_long_table_names;

[Collection(PostgreSqlCollection.Name)]
public class and_the_provider_is_postgresql(PostgreSqlFixture fixture) : given.a_long_replayed_read_model<PostgreSqlSinkHarness>
{
    private protected override IReadModelTables Tables => _sqlHarness.Tables;

    protected override PostgreSqlSinkHarness CreateSqlHarness() => new() { Fixture = fixture };

    Task Because() => PromoteTwice();

    [Fact] void should_publish_the_second_replay() => _current.ShouldEqual(3);
    [Fact] void should_keep_every_physical_identifier_within_the_byte_limit() =>
        new[] { _liveName, _replayName, _firstBackupName, _secondBackupName }.All(name => Encoding.UTF8.GetByteCount(name) <= 63).ShouldBeTrue();
    [Fact] void should_not_collapse_roles_or_occurrences() =>
        new[] { _liveName, _replayName, _firstBackupName, _secondBackupName }.Distinct().Count().ShouldEqual(4);
}
