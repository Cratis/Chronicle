// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;
using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2;

public partial class when_running_the_sqlite_contract : given.a_sqlite_v2_storage
{
    [Fact]
    public async Task should_not_provision_any_schema_from_missing_reads()
    {
        var before = await Catalog();
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual(Option<UniqueConstraintWriteAuthority>.None());
        (await _storage.IsAllowed(_owner, _definition, _value, _scope)).IsAllowed.ShouldBeTrue();
        (await _storage.GetFor(_owner, _definition, _scope)).ShouldBeEmpty();
        var count = 0;
        await foreach (var value in await _storage.GetForStream(_owner, _definition, _scope))
        {
            count++;
        }

        count.ShouldEqual(0);
        (await Catalog()).SequenceEqual(before).ShouldBeTrue();
    }

    [Fact]
    public async Task should_not_provision_claims_or_authority_from_refused_mutations()
    {
        var before = await Catalog();
        for (var operation = 0; operation < 6; operation++)
        {
            ShouldRefuse(await Mutation(operation, _authority), UniqueConstraintWriteFailure.MissingAuthority);
        }

        (await Catalog()).SequenceEqual(before).ShouldBeTrue();
    }

    [Fact]
    public async Task should_reuse_lazy_schema_and_create_a_separate_table_for_each_mode()
    {
        await Seed();
        var before = await Catalog();
        (await Handle().Save(_owner, _definition, 99, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await Catalog()).SequenceEqual(before).ShouldBeTrue();
        (await _storage.Save(_owner, PerValue, 10, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        var names = await Catalog();
        names.Length.ShouldBeGreaterThan(before.Length);
        names.Where(name => name.Contains("CREATE TABLE", StringComparison.Ordinal)).Count().ShouldEqual(4);
        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_coordinate_racing_lazy_table_creation_on_independent_connections()
    {
        await Initialize();
        var other = Handle();
        var results = await Task.WhenAll(
            Task.Run(() => _storage.Save(_owner, _definition, 10, _value, _scope, _authority)),
            Task.Run(() => other.Save(_otherOwner, _definition, 11, _otherValue, _scope, _authority))).WaitAsync(TimeSpan.FromSeconds(40));
        results.All(result => result.IsSuccess).ShouldBeTrue();
        await ShouldRetainClaim();
        (await other.GetFor(_otherOwner, _definition, _scope)).ShouldContainOnly(_otherValue);
    }

    [Fact]
    public async Task should_isolate_namespace_databases_without_changing_logical_identities()
    {
        await Seed();
        var connection = new SqliteConnectionStringBuilder(_connectionString);
        connection.DataSource += ".other";
        var other = Handle(connectionString: connection.ConnectionString);
        ShouldRefuse(await other.Save(_otherOwner, _definition, 99, _value, _scope, _authority), UniqueConstraintWriteFailure.MissingAuthority);
        (await other.InitializeAuthority(_definition.Name, _authority)).IsSuccess.ShouldBeTrue();
        (await other.Save(_otherOwner, _definition, 11, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        await ShouldRetainClaim();
    }
}
