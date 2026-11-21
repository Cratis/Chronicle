// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator;

public class when_configuring_claim_tables : Specification
{
    const string Ordinal = "Latin1_General_100_BIN2";
    readonly List<(string?[] Collations, string Script)> _sqlServer = [];
    readonly List<string?> _otherProviders = [];

    void Because()
    {
        Check(builder => builder.UseSqlServer("Server=localhost;Database=model-only;Integrated Security=true"), true);
        Check(builder => builder.UseSqlite("DataSource=:memory:"), false);
        Check(builder => builder.UseNpgsql("Host=localhost;Database=model_only"), false);
    }

    [Fact]
    void should_use_ordinal_collations_on_both_identity_columns_in_both_sql_server_models()
    {
        foreach (var (collations, _) in _sqlServer) collations.ShouldEqual([Ordinal, Ordinal]);
    }

    [Fact]
    void should_emit_both_sql_server_column_collations()
    {
        foreach (var (_, script) in _sqlServer) (script.Split($"COLLATE {Ordinal}").Length - 1).ShouldEqual(2);
    }

    [Fact]
    void should_not_change_other_provider_collations()
    {
        foreach (var collation in _otherProviders) collation.ShouldBeNull();
    }

    void Check(Action<DbContextOptionsBuilder> configure, bool sqlServer)
    {
        using var sources = new UniqueConstraintDbContext(Options<UniqueConstraintDbContext>(configure), "source_claims", Substitute.For<IUniqueConstraintMigrator>());
        using var values = new UniqueConstraintValuesDbContext(Options<UniqueConstraintValuesDbContext>(configure), "value_claims", Substitute.For<IUniqueConstraintMigrator>());
        Capture(sources, typeof(UniqueConstraintIndexEntry), sqlServer);
        Capture(values, typeof(UniqueConstraintValueEntry), sqlServer);
    }

    void Capture(DbContext context, Type entry, bool sqlServer)
    {
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(entry)!;
        var collations = new[] { entity.FindProperty("EventSourceId")!.GetCollation(), entity.FindProperty("Value")!.GetCollation() };
        if (sqlServer) _sqlServer.Add((collations, context.Database.GenerateCreateScript()));
        else _otherProviders.AddRange(collations);
    }

    static DbContextOptions<T> Options<T>(Action<DbContextOptionsBuilder> configure)
        where T : DbContext
    {
        var builder = new DbContextOptionsBuilder<T>();
        configure(builder);
        return builder.AddConceptAsSupport().WithoutServiceProviderCaching().Options;
    }
}
