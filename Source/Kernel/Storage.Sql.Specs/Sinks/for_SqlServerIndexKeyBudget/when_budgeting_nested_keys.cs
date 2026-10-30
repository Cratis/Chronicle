// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SqlServerIndexKeyBudget;

public class when_budgeting_nested_keys : Specification
{
    JsonSchema _schema;
    IReadOnlyList<ProjectedColumn> _columns;
    ReadModelDbContext _context;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"details":{"type":"object","properties":{"count":{"type":"integer"},"total":{"type":"number"},"active":{"type":"boolean"}}}}}""");
        _columns = ProjectedColumns.ForSchema(_schema);
        _context = new(new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlServer("Server=localhost;Database=indexes").Options, "indexes", _columns, Substitute.For<IReadModelMigrator>());
    }

    [Fact] async Task should_allow_one_nested_number_key() => (await Rejection(new("details.total"))).ShouldBeNull();
    [Fact] async Task should_allow_one_nested_integer_key() => (await Rejection(new("details.count"))).ShouldBeNull();
    [Fact] async Task should_allow_one_nested_boolean_key() => (await Rejection(new("details.active"))).ShouldBeNull();
    [Fact] async Task should_budget_numbers_as_900_bytes_each() => (await Rejection(new("details.total") { Properties = [new("details.total"), new("details.count")] })).ShouldEqual("the combined key exceeds the SQL Server nonclustered key limit of 1700 bytes");
    [Fact] async Task should_budget_booleans_as_900_bytes_each() => (await Rejection(new("details.active") { Properties = [new("details.active"), new("details.count")] })).ShouldEqual("the combined key exceeds the SQL Server nonclustered key limit of 1700 bytes");

    void Destroy() => _context.Dispose();

    Task<string?> Rejection(IndexDefinition definition) => SqlServerIndexKeyBudget.RejectionReason(_context, "indexes", ReadModelIndexPlan.Resolve(definition, _schema, _columns, DatabaseType.SqlServer)!);
}
