// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Security.for_DataProtectionKeyRingWarmup;

public class when_starting_an_ordinary_host : Specification
{
    bool _routed;
    CancellationToken _startupToken;
    IHostedService _framework;
    IHostedService _silo;
    IHost _host;

    void Establish()
    {
        _framework = Substitute.For<IHostedService>();
        _silo = Substitute.For<IHostedService>();
        _silo.StartAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _routed = true;
            return Task.CompletedTask;
        });
        _framework.StartAsync(Arg.Any<CancellationToken>()).Returns(call =>
        {
            _routed.ShouldBeTrue();
            _startupToken = call.Arg<CancellationToken>();
            return Task.CompletedTask;
        });
        _host = new HostBuilder().ConfigureServices(services =>
        {
            // Deliberately register the warmup before the silo.
            services.AddSingleton<IHostedService>(new DataProtectionKeyRingWarmup(_framework));
            services.AddSingleton(_silo);
        }).Build();
    }

    async Task Because()
    {
        await _host.StartAsync();
        await _host.StopAsync();
    }

    void Destroy() => _host.Dispose();

    [Fact] void should_run_the_framework_warmup_once_after_routing() => _framework.Received(1).StartAsync(_startupToken);
    [Fact] void should_preserve_framework_stop() => _framework.Received(1).StopAsync(Arg.Any<CancellationToken>());
}
