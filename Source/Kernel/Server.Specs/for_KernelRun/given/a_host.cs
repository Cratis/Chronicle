// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.for_KernelRun.given;

/// <summary>
/// A real generic host whose stop is requested as soon as it has started.
/// </summary>
/// <remarks>
/// The host gets a generous shutdown timeout, so a slow build agent cannot turn a clean or failing stop into a
/// timeout. Only a spec about overrunning the timeout passes the short <see cref="_shutdownTimeout"/>.
/// </remarks>
public class a_host : Specification
{
    protected internal static readonly TimeSpan _shutdownTimeout = TimeSpan.FromMilliseconds(300);
    protected static readonly TimeSpan _generousShutdownTimeout = TimeSpan.FromSeconds(30);
    protected CancellationTokenSource _stopRequest;
    protected IHost _host;

    void Establish() => _stopRequest = new();

    void Destroy() => _stopRequest.Dispose();

    protected void BuildHost(Action<IServiceCollection> configure, TimeSpan? shutdownTimeout = null)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new());
        builder.Services.Configure<HostOptions>(_ => _.ShutdownTimeout = shutdownTimeout ?? _generousShutdownTimeout);
        configure(builder.Services);
        _host = builder.Build();
        _host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(_stopRequest.Cancel);
    }

    internal Task<KernelRunResult> Run() => KernelRun.RunAsync(_host, _stopRequest.Token);
}
