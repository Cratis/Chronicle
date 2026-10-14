// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Occurrences.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Occurrences.for_OccurrenceMigrations;

public class when_listing_the_namespace_migration : Specification
{
    string _id;
    string _previous;
    Type _context;

    void Because()
    {
        _id = typeof(v19_66_0).GetCustomAttribute<MigrationAttribute>()!.Id;
        _previous = typeof(EventSequences.Migrations.v15_0_0).GetCustomAttribute<MigrationAttribute>()!.Id;
        _context = typeof(v19_66_0).GetCustomAttribute<DbContextAttribute>()!.ContextType;
    }

    [Fact] void should_use_the_namespace_prefix() => _id.ShouldEqual("NS-Occurrences-v19_66_0");
    [Fact] void should_bind_the_namespace_context() => _context.ShouldEqual(typeof(NamespaceDbContext));
    [Fact] void should_sort_after_the_namespace_parent_table() => string.CompareOrdinal(_id, _previous).ShouldBeGreaterThan(0);
}
