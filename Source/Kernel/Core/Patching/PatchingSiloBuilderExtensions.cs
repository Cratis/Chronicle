// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Hosting;
using Cratis.Choreography.Storage.Patching;
using Cratis.Chronicle.Sys;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Extension methods for adding patching to a Chronicle silo.
/// </summary>
public static class PatchingSiloBuilderExtensions
{
    /// <summary>
    /// Adds patching with Cratis Choreography, keeping the record of applied patches in Chronicle's system storage.
    /// </summary>
    /// <param name="builder">The <see cref="ISiloBuilder"/> to add to.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    /// <remarks>
    /// Patches are applied by the startup task, in the order it has chosen for them, so the startup task of Cratis Choreography
    /// that would apply them as the silo becomes active is not added.
    /// </remarks>
    public static ISiloBuilder AddChroniclePatching(this ISiloBuilder builder)
    {
        builder.Services.TryAddSingleton<IPatchStorage, ChroniclePatchStorage>();
        return builder.AddCratisChoreographyPatchingServices(options => options.HostVersion = KernelVersion.Current.ToPatching());
    }
}
