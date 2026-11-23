// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2;

public partial class when_running_the_sqlite_contract
{
    [Fact]
    public async Task should_leave_real_v1_claims_metadata_and_schema_unchanged()
    {
        await using var metadata = new EventStoreDbContext(new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlite(_connectionString).AddConceptAsSupport().Options);
        await metadata.Database.EnsureCreatedAsync();
        metadata.UniqueConstraintEncodingKey.Add(new() { Id = 1, KeyId = new byte[16], Material = Enumerable.Repeat((byte)7, 32).ToArray() });
        await metadata.SaveChangesAsync();
        var migrator = new UniqueConstraintMigrator(
            new TableMigrator<UniqueConstraintDbContext>(NullLogger<TableMigrator<UniqueConstraintDbContext>>.Instance),
            new TableMigrator<UniqueConstraintValuesDbContext>(NullLogger<TableMigrator<UniqueConstraintValuesDbContext>>.Instance),
            NullLogger<UniqueConstraintMigrator>.Instance);
        await using var owners = new UniqueConstraintDbContext(new DbContextOptionsBuilder<UniqueConstraintDbContext>().UseSqlite(_connectionString).AddConceptAsSupport().Options, "log_constraint_constraint", migrator);
        await using var values = new UniqueConstraintValuesDbContext(new DbContextOptionsBuilder<UniqueConstraintValuesDbContext>().UseSqlite(_connectionString).AddConceptAsSupport().Options, "log_constraint_constraint_values", migrator);
        await owners.EnsureTableExists();
        await values.EnsureTableExists();
        owners.Entries.Add(new() { EventSourceId = "v1-owner", Value = "plaintext-v1", SequenceNumber = 7, Subject = "v1-subject" });
        values.Entries.Add(new() { EventSourceId = "v1-owner", Value = "plaintext-v1", SequenceNumber = 8, Subject = "v1-subject" });
        await owners.SaveChangesAsync();
        await values.SaveChangesAsync();
        var before = await Catalog();
        await Seed();
        (await _storage.Save(_owner, PerValue, 10, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.RemoveAllFor(_definition, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.RemoveAllFor(PerValue, _authority)).IsSuccess.ShouldBeTrue();
        var after = await Catalog();
        before.All(after.Contains).ShouldBeTrue();
        var owner = await owners.Entries.AsNoTracking().SingleAsync();
        owner.EventSourceId.ShouldEqual("v1-owner");
        owner.Value.ShouldEqual("plaintext-v1");
        owner.SequenceNumber.ShouldEqual(7m);
        owner.Subject.ShouldEqual("v1-subject");
        var value = await values.Entries.AsNoTracking().SingleAsync();
        value.EventSourceId.ShouldEqual("v1-owner");
        value.Value.ShouldEqual("plaintext-v1");
        value.SequenceNumber.ShouldEqual(8m);
        value.Subject.ShouldEqual("v1-subject");
        var key = await metadata.UniqueConstraintEncodingKey.AsNoTracking().SingleAsync();
        key.KeyId.SequenceEqual(new byte[16]).ShouldBeTrue();
        key.Material.SequenceEqual(Enumerable.Repeat((byte)7, 32)).ShouldBeTrue();
    }
}
