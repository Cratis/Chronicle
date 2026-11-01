// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverReplayCompletionMigration;

public class when_downgrading_sql_server : Specification, IDisposable
{
    NamespaceDbContext _context;
    IReadOnlyList<MigrationOperation> _operations;
    string _sql;

    void Establish() => _context = new(new DbContextOptionsBuilder<NamespaceDbContext>()
        .UseSqlServer("Server=localhost;Database=unused;Integrated Security=true;TrustServerCertificate=true")
        .AddConceptAsSupport()
        .WithoutServiceProviderCaching()
        .Options);

    void Because()
    {
        _operations = new Migrations.v19_116_0_1 { ActiveProvider = "Microsoft.EntityFrameworkCore.SqlServer" }.DownOperations;
        _sql = string.Join('\n', _context.GetService<IMigrationsSqlGenerator>().Generate(_operations).Select(command => command.CommandText));
    }

    [Fact] void should_only_drop_the_completion_column_and_its_check() => _operations.Count.ShouldEqual(2);
    [Fact] void should_only_drop_the_completion_column() => _operations.OfType<DropColumnOperation>().Select(column => column.Name).ShouldContainOnly("PartitionReplayCompletions");
    [Fact] void should_only_drop_the_completion_json_check() => _operations.OfType<DropCheckConstraintOperation>().Select(constraint => constraint.Name).ShouldContainOnly("CK_Observers_PartitionReplayCompletions_IsJson");
    [Fact] void should_drop_the_completion_column() => _sql.ShouldContain("DROP COLUMN [PartitionReplayCompletions]");
    [Fact] void should_leave_the_owner_column() => _sql.Contains("DROP COLUMN [Owner]", StringComparison.Ordinal).ShouldBeFalse();

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
