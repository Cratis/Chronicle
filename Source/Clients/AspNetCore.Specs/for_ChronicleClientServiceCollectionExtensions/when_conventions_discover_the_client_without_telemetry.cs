// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class when_conventions_discover_the_client_without_telemetry : Specification
{
    ServiceProvider _provider;
    IChronicleClient _client;
    ChronicleAspNetCoreOptions _options;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddTypeDiscovery();
        builder.Services.AddSelfBindings();
        builder.Services.AddBindingsByConvention();
        builder.AddCratisChronicle();
        _provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _options = _provider.GetRequiredService<IOptions<ChronicleAspNetCoreOptions>>().Value;
        _client = _provider.GetRequiredService<IChronicleClient>();
    }

    void Destroy() => _provider?.Dispose();

    [Fact] void should_resolve_the_client() => _client.ShouldBeOfExactType<ChronicleClient>();
    [Fact] void should_keep_default_identifier_tracing() => _options.Telemetry.EventSourceIdTracing.ShouldEqual(EventSourceIdTracing.Raw);
    [Fact] void should_not_bind_a_telemetry_callback() => _provider.GetServices<ChronicleTelemetryConfiguration>().ShouldBeEmpty();
}
