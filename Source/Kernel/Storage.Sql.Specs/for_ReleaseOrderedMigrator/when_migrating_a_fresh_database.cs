// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ReleaseOrderedMigrator;

public class when_migrating_a_fresh_database : given.a_context_with_migrations_from_three_releases
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _context.Database.MigrateAsync());

    [Fact] void should_apply_every_migration() => _error.ShouldBeNull();
    [Fact] void should_apply_the_migrations_in_release_order() =>
        AppliedMigrations().ShouldEqual(["Releases-v19_9_0", "Releases-v19_15_0", "Releases-v19_104_0"]);
}
