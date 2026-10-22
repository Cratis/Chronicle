// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelDbContext;

public class when_mapping_indexed_sql_server_strings : Specification
{
    ReadModelDbContext _context;
    Dictionary<string, string> _types;
    string _changedType;
    string _unindexedType;
    DbContextOptions<ReadModelDbContext> _options;

    void Establish()
    {
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"},"bounded":{"type":"string","maxLength":80},"indexed":{"type":"string"},"large":{"type":"string","maxLength":900},"unbounded":{"type":"string"},"explicitOnly":{"type":"string","maxLength":40}}}""");
        var columns = ProjectedColumns.ForSchema(schema, new[] { "bounded", "indexed", "large" }.Select(name => new IndexDefinition(new PropertyPath(name))));
        _options = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlServer("Server=localhost;Database=not-opened;Integrated Security=true;TrustServerCertificate=true").Options;
        var migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()), Substitute.For<ILogger<ReadModelMigrator>>());
        _context = new ReadModelDbContext(_options, "strings", columns, migrator);
    }

    void Because()
    {
        _types = _context.Model.FindEntityType("strings")!.GetProperties().ToDictionary(property => property.Name, property => property.GetColumnType()!);
        using var changed = new ReadModelDbContext(_options, "strings", _context.Columns.Select(column => column.Name == "bounded" ? column with { MaxLength = 160 } : column).ToArray(), Substitute.For<IReadModelMigrator>());
        _changedType = changed.Model.FindEntityType("strings")!.FindProperty("bounded")!.GetColumnType()!;
        using var unindexed = new ReadModelDbContext(_options, "strings", _context.Columns.Select(column => column with { IsIndexed = false }).ToArray(), Substitute.For<IReadModelMigrator>());
        _unindexedType = unindexed.Model.FindEntityType("strings")!.FindProperty("indexed")!.GetColumnType()!;
    }
    void Destroy() => _context.Dispose();

    [Fact] void should_refresh_the_model_when_a_bound_changes() => _changedType.ShouldEqual("NVARCHAR(160)");
    [Fact] void should_refresh_the_model_when_an_index_is_removed() => _unindexedType.ShouldEqual("NVARCHAR(MAX)");
    [Fact] void should_preserve_primary_key_sizing() => _types["id"].ShouldEqual("NVARCHAR(200)");
    [Fact] void should_preserve_an_explicit_bound_without_an_index() => _types["explicitOnly"].ShouldEqual("NVARCHAR(40)");
    [Fact] void should_use_an_explicit_bound() => _types["bounded"].ShouldEqual("NVARCHAR(80)");
    [Fact] void should_bound_an_indexed_string_without_a_declared_length() => _types["indexed"].ShouldEqual("NVARCHAR(450)");
    [Fact] void should_leave_oversized_declarations_unbounded() => _types["large"].ShouldEqual("NVARCHAR(MAX)");
    [Fact] void should_leave_nonindexed_strings_unbounded() => _types["unbounded"].ShouldEqual("NVARCHAR(MAX)");
}
