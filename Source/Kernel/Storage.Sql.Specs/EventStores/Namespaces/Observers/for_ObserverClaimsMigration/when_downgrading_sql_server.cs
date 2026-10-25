// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverClaimsMigration;

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
        _operations = new Migrations.v19_113_0 { ActiveProvider = "Microsoft.EntityFrameworkCore.SqlServer" }.DownOperations;
        _sql = string.Join('\n', _context.GetService<IMigrationsSqlGenerator>().Generate(_operations).Select(command => command.CommandText));
    }

    [Fact] void should_only_drop_shadow_columns_and_their_checks() => _operations.All(operation => operation is DropColumnOperation or DropCheckConstraintOperation).ShouldBeTrue();
    [Fact] void should_only_drop_the_three_shadow_columns() => _operations.OfType<DropColumnOperation>().Select(column => column.Name).ShouldContainOnly("PartitionClaims", "ObserverWideClaim", "OwnershipRevision");
    [Fact] void should_drop_only_shadow_json_checks() => _operations.OfType<DropCheckConstraintOperation>().Select(constraint => constraint.Name).ShouldContainOnly("CK_Observers_PartitionClaims_IsJson", "CK_Observers_ObserverWideClaim_IsJson");
    [Fact] void should_drop_partition_claims() => _sql.ShouldContain("DROP COLUMN [PartitionClaims]");
    [Fact] void should_drop_observer_wide_claim() => _sql.ShouldContain("DROP COLUMN [ObserverWideClaim]");
    [Fact] void should_drop_ownership_revision() => _sql.ShouldContain("DROP COLUMN [OwnershipRevision]");
    [Fact] void should_leave_the_owner_column() => _sql.Contains("DROP COLUMN [Owner]", StringComparison.Ordinal).ShouldBeFalse();

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
