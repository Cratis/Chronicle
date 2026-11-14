// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.for_SpecDbContextOptions.when_configuring_interceptors;

public class without_interceptors : Specification
{
    CoreOptionsExtension _options;

    void Because() => _options = new DbContextOptionsBuilder()
        .UseSqlite("Data Source=:memory:")
        .WithSpecInterceptors([])
        .Options.FindExtension<CoreOptionsExtension>()!;

    [Fact] void should_keep_provider_caching() => _options.ServiceProviderCachingEnabled.ShouldBeTrue();
}
