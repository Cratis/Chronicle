// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.for_SpecDbContextOptions;

public class when_requesting_uncached_options : Specification
{
    CoreOptionsExtension _cached;
    CoreOptionsExtension _uncached;
    Exception _warning;

    void Because()
    {
        var builder = new DbContextOptionsBuilder<DbContext>(SpecDbContextOptions.ForConnection<DbContext>("Data Source=uncached-options;Mode=Memory;Cache=Shared"));
        _cached = builder.Options.GetExtension<CoreOptionsExtension>();
        var options = builder.WithoutServiceProviderCaching().Options;
        _uncached = options.GetExtension<CoreOptionsExtension>();
        using var context = new DbContext(options);
        _warning = Catch.Exception(() => context.GetService<IDiagnosticsLogger<DbLoggerCategory.Infrastructure>>().ManyServiceProvidersCreatedWarning([]));
    }

    [Fact] void should_disable_service_provider_caching() => _uncached.ServiceProviderCachingEnabled.ShouldBeFalse();
    [Fact] void should_log_the_process_wide_provider_diagnostic_for_cached_options() => _cached.WarningsConfiguration.GetBehavior(CoreEventId.ManyServiceProvidersCreatedWarning).ShouldEqual(WarningBehavior.Log);
    [Fact] void should_log_the_intentional_uncached_provider_diagnostic() => _uncached.WarningsConfiguration.GetBehavior(CoreEventId.ManyServiceProvidersCreatedWarning).ShouldEqual(WarningBehavior.Log);
    [Fact] void should_not_turn_that_diagnostic_into_a_fixture_failure() => _warning.ShouldBeNull();
    [Fact] void should_keep_other_warning_guards() => _uncached.WarningsConfiguration.GetBehavior(CoreEventId.InvalidIncludePathError).ShouldEqual(WarningBehavior.Throw);
}
