// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelRun.given;

/// <summary>
/// A real generic host with a short shutdown timeout, whose stop is requested as soon as it has started.
/// </summary>
public class a_host : Specification
{
    protected internal static readonly TimeSpan _shutdownTimeout = TimeSpan.FromMilliseconds(300);
    protected CancellationTokenSource _stopRequest;
    protected IHost _host;

    void Establish() => _stopRequest = new();

    void Destroy() => _stopRequest.Dispose();

    protected void BuildHost(Action<IServiceCollection> configure)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new());
        builder.Services.Configure<HostOptions>(_ => _.ShutdownTimeout = _shutdownTimeout);
        configure(builder.Services);
        _host = builder.Build();
        _host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(_stopRequest.Cancel);
    }

    internal Task<KernelRunResult> Run() => KernelRun.RunAsync(_host, _stopRequest.Token);
}
