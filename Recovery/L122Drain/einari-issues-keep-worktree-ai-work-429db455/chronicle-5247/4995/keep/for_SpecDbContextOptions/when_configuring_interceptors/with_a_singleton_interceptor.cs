// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.for_SpecDbContextOptions.when_configuring_interceptors;

public class with_a_singleton_interceptor : Specification
{
    SqlReadCapture _interceptor;
    CoreOptionsExtension _options;

    void Establish() => _interceptor = new SqlReadCapture();

    void Because() => _options = new DbContextOptionsBuilder()
        .UseSqlite("Data Source=:memory:")
        .WithSpecInterceptors([_interceptor])
        .Options.FindExtension<CoreOptionsExtension>()!;

    [Fact] void should_disable_provider_caching() => _options.ServiceProviderCachingEnabled.ShouldBeFalse();
    [Fact] void should_register_the_interceptor() => _options.Interceptors!.ShouldContain(_interceptor);
}
