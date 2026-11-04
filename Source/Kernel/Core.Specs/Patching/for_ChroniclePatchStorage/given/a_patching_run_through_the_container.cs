// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ChoreographyPatchManager = Cratis.Choreography.Patching.PatchManager;

namespace Cratis.Chronicle.Patching.for_ChroniclePatchStorage.given;

/// <summary>
/// The patching run of <see cref="a_patching_run_with_a_quarantined_event_store"/>, with the patch manager, the patch storage and the
/// patch all resolved from a container wired the way the silo wires them, so the patch the manager runs and the one the patch storage
/// asks about its skipped resources are whatever the container hands out.
/// </summary>
public class a_patching_run_through_the_container : a_patching_run_with_a_quarantined_event_store
{
    protected ServiceProvider _services;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_storage);
        services.AddSingleton(_grainFactory);
        services.AddSingleton<IResourceNameSnapshot>(_resourceNames);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var types = Substitute.For<ITypes>();
        types.FindMultiple<ICanApplyPatch>().Returns([typeof(RemoveUnhashedUniqueConstraintValues)]);
        services.AddSingleton(types);

        var silo = Substitute.For<ISiloBuilder>();
        silo.Services.Returns(services);
        silo.AddChroniclePatching([typeof(RemoveUnhashedUniqueConstraintValues)]);
        services.Configure<PatchingOptions>(options => options.HostVersion = new SemanticVersion(19, 118, 0));

        _services = services.BuildServiceProvider();
        _manager = ActivatorUtilities.CreateInstance<ChoreographyPatchManager>(_services);
    }

    void Destroy() => _services.Dispose();
}
