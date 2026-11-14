// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.given;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock;

public class when_creating_a_spec_context : Specification
{
    CoreOptionsExtension _options;
    bool _reusedProvider;

    void Because()
    {
        using var first = CreateContext("first.db");
        using var second = CreateContext("second.db");
        _options = first.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>()!;
        _reusedProvider = ReferenceEquals(first.GetService<IModelSource>(), second.GetService<IModelSource>());
    }

    [Fact] void should_cache_the_internal_provider() => _options.ServiceProviderCachingEnabled.ShouldBeTrue();
    [Fact] void should_reuse_the_provider_across_databases() => _reusedProvider.ShouldBeTrue();
    [Fact] void should_not_suppress_the_provider_limit_warning() => _options.WarningsConfiguration.GetBehavior(CoreEventId.ManyServiceProvidersCreatedWarning).ShouldEqual(WarningBehavior.Throw);

    static SchemaLockDbContext CreateContext(string database) => new(new DbContextOptionsBuilder<SchemaLockDbContext>()
        .UseSqlite($"Data Source={database}")
        .Options);
}
