// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints.given;

/// <summary>
/// An Arc command pipeline in a host the kernel shares with an application whose command scope, filter and
/// deny-by-default authorization are all discovered.
/// </summary>
public class a_host_shared_with_an_application : Specification, IDisposable
{
    protected ServiceProvider _provider;
    protected ICommandPipeline _pipeline;
    protected ApplicationExtensionPointsRecorder _recorder;

    void Establish()
    {
        _recorder = new();
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.Configure<ArcOptions>(_ => { });
        services.AddCratisArcCore();
        services.AddKernelSideCommandExtensionPointsForKernelCommands();
        services.AddSingleton(_recorder);
        _provider = services.BuildServiceProvider();
        _pipeline = _provider.GetRequiredService<ICommandPipeline>();
    }

    public void Dispose() => _provider.Dispose();
}
