// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Schema.for_SchemaVerification.when_verifying;

public class and_history_has_a_migration_this_version_does_not_know : Specification
{
    Exception _result;

    void Because()
    {
        // History rows from a newer version are unknown here; verification only looks for known migrations that are missing.
        var assembly = Substitute.For<IMigrationsAssembly>();
        assembly.Migrations.Returns(new Dictionary<string, TypeInfo>
        {
            ["Cluster-Known-v1_0_0"] = typeof(object).GetTypeInfo()
        });

        var history = Substitute.For<IHistoryRepository>();
        history.GetAppliedMigrations().Returns(
        [
            new HistoryRow("Cluster-Known-v1_0_0", "10.0.0"),
            new HistoryRow("Cluster-FromANewerVersion-v99_0_0", "10.0.0")
        ]);

        var creator = Substitute.For<IDatabaseCreator, IRelationalDatabaseCreator>();
        ((IRelationalDatabaseCreator)creator).Exists().Returns(true);

        var verification = new SchemaVerification(
            Substitute.For<IDbContextOptions>(),
            assembly,
            history,
            creator,
            Substitute.For<IRelationalConnection>(),
            Substitute.For<ICurrentDbContext>());

        _result = Catch.Exception(verification.Verify);
    }

    [Fact] void should_not_throw() => _result.ShouldBeNull();
}
