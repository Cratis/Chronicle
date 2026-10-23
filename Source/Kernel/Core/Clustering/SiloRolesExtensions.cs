// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Hosting;
using Orleans.Runtime.MembershipService.SiloMetadata;

namespace Cratis.Chronicle.Clustering;

/// <summary>
/// Configures role advertisement and validation for Chronicle silos.
/// </summary>
public static class SiloRolesExtensions
{
    /// <summary>
    /// Adds Chronicle roles to silo metadata without replacing host metadata.
    /// </summary>
    /// <param name="builder">The silo builder.</param>
    /// <returns>The builder for continuation.</returns>
    public static ISiloBuilder AddChronicleSiloRoles(this ISiloBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ChronicleOptions>, ClusterRolesValidator>());
        builder.Services.AddOptions<ChronicleOptions>().ValidateOnStart();
        builder.Services.TryAddSingleton<ISiloRoles, SiloRoles>();

        // Orleans accepts a dictionary, not an options factory. Populate it from the final Chronicle options
        // immediately before Orleans merges it into SiloMetadata; do not snapshot options during registration.
        var metadata = new Dictionary<string, string>();
        builder.Services.AddOptions<SiloMetadata>().Configure<IOptions<ChronicleOptions>>((_, options) =>
        {
            var roles = options.Value.Clustering.Roles;
            metadata[SiloRoles.VersionKey] = "1";
            metadata[SiloRoles.EventSequencesKey] = roles.EventSequences ? "true" : "false";
            metadata[SiloRoles.ObserversKey] = roles.Observers ? "true" : "false";
            metadata[SiloRoles.JobsKey] = roles.Jobs ? "true" : "false";
        });
        return builder.UseSiloMetadata(metadata);
    }
}
