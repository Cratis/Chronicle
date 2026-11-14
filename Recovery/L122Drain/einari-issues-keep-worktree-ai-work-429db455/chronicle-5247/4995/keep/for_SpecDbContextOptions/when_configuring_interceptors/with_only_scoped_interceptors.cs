// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.for_SpecDbContextOptions.when_configuring_interceptors;

public class with_only_scoped_interceptors : Specification
{
    IDbCommandInterceptor _interceptor;
    CoreOptionsExtension _options;

    void Establish() => _interceptor = Substitute.For<IDbCommandInterceptor>();

    void Because() => _options = new DbContextOptionsBuilder()
        .UseSqlite("Data Source=:memory:")
        .WithSpecInterceptors([_interceptor])
        .Options.FindExtension<CoreOptionsExtension>()!;

    [Fact] void should_keep_provider_caching() => _options.ServiceProviderCachingEnabled.ShouldBeTrue();
    [Fact] void should_register_the_interceptor() => _options.Interceptors!.ShouldContain(_interceptor);
}
