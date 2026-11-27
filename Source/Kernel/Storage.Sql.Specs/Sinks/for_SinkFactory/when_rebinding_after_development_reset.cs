// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkFactory;

public class when_rebinding_after_development_reset : Specification
{
    ISinkProvenanceStorage _before;
    ISinkProvenanceStorage _after;
    Exception? _oldError;

    async Task Because()
    {
        await using var cache = new DevelopmentSinkProvenanceCache();
        await using var services = new ServiceCollection()
            .AddSingleton(Substitute.For<IDatabase>())
            .AddSingleton<ISinkProvenanceCache>(cache)
            .BuildServiceProvider();
        var factory = new SinkFactory(services, Substitute.For<IExpandoObjectConverter>());
        _before = factory.GetProvenance("store", "scope", SinkName.Default, "model");
        await cache.RotateAfterWipe();
        _after = factory.GetProvenance("store", "scope", SinkName.Default, "model");
        _oldError = await Catch.Exception(async () => await _before.GetLease());
    }

    [Fact] void should_rebind_the_same_route_on_the_same_factory() => _after.ShouldNotEqual(_before);
    [Fact] void should_not_reactivate_the_disposed_handle() => _oldError.ShouldBeOfExactType<ObjectDisposedException>();
}
#endif
