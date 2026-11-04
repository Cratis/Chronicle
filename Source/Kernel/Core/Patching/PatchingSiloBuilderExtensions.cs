// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Hosting;
using Cratis.Choreography.Patching;
using Cratis.Choreography.Storage.Patching;
using Cratis.Chronicle.Sys;
using Microsoft.Extensions.DependencyInjection;
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
    /// that would apply them as the silo becomes active is not added. The patches that report skipped resources are found in the
    /// current type universe rather than in the snapshot <c language="csharp">Types.Instance</c> took on first use, which can miss assemblies loaded since.
    /// </remarks>
    public static ISiloBuilder AddChroniclePatching(this ISiloBuilder builder) =>
        builder.AddChroniclePatching(TypesServiceCollectionExtensions.CurrentTypeUniverse().FindMultiple<IReportSkippedResources>());

    /// <summary>
    /// Adds patching with Cratis Choreography, registering every patch among the given types that reports skipped resources as
    /// one shared instance, resolved as <see cref="ICanApplyPatch"/> as well as by its own type.
    /// </summary>
    /// <param name="builder">The <see cref="ISiloBuilder"/> to add to.</param>
    /// <param name="types">The candidate patch types.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    /// <remarks>
    /// The patches Cratis Choreography enumerates are the registered <see cref="ICanApplyPatch"/> services, plus a new instance of
    /// every other implementation it discovers, created again on each enumeration. The skipped resources a patch reports
    /// live on the instance that ran, and <see cref="ChroniclePatchStorage"/> reads them from the instance it enumerates,
    /// so the two must be the same one, or a patch that left resources out is recorded as applied.
    /// </remarks>
    internal static ISiloBuilder AddChroniclePatching(this ISiloBuilder builder, IEnumerable<Type> types)
    {
        builder.Services.TryAddSingleton<IPatchStorage, ChroniclePatchStorage>();
        AddPatchesThatReportSkippedResources(builder.Services, types);
        return builder.AddCratisChoreographyPatchingServices(options => options.HostVersion = KernelVersion.Current.ToPatching());
    }

    static void AddPatchesThatReportSkippedResources(IServiceCollection services, IEnumerable<Type> types)
    {
        var patches = types.Where(_ => _ is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } &&
            _.IsAssignableTo(typeof(ICanApplyPatch)) &&
            _.IsAssignableTo(typeof(IReportSkippedResources)));
        foreach (var patch in patches.Distinct())
        {
            services.TryAddSingleton(patch);
            services.AddSingleton(sp => (ICanApplyPatch)sp.GetRequiredService(patch));
        }
    }
}
