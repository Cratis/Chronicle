// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class when_configuring_hashed_identifier_tracing : Specification
{
    const string HashKey = "01234567890123456789012345678901";
    ServiceProvider _provider;
    ChronicleAspNetCoreOptions _options;

    void Establish()
    {
        var services = new ServiceCollection();
        var builder = new ChronicleBuilder(services, new ConfigurationBuilder().Build(), DefaultClientArtifactsProvider.Default);
        builder.WithTelemetry(options =>
        {
            options.EventSourceIdTracing = EventSourceIdTracing.Hashed;
            options.EventSourceIdHashKey = HashKey;
        });
        services.AddCratisChronicleClient(builder);
        _provider = services.BuildServiceProvider();
    }

    void Because() => _options = _provider.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value;

    void Destroy() => _provider.Dispose();

    [Fact] void should_apply_the_tracing_mode_to_host_options() => _options.Telemetry.EventSourceIdTracing.ShouldEqual(EventSourceIdTracing.Hashed);
    [Fact] void should_apply_the_hash_key_to_host_options() => _options.Telemetry.EventSourceIdHashKey.ShouldEqual(HashKey);
}
