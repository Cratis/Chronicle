// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Applies Chronicle's opt-in grain directory configuration to Orleans.
/// </summary>
internal static class GrainDirectoryConfiguration
{
    /// <summary>
    /// Registers the experimental distributed grain directory only when explicitly enabled.
    /// </summary>
    /// <param name="builder">The silo builder to configure.</param>
    /// <returns>The silo builder for continuation.</returns>
    internal static ISiloBuilder AddConfiguredGrainDirectory(this ISiloBuilder builder)
    {
        var key = ConfigurationPath.Combine(ChronicleOptions.SectionPath, nameof(ChronicleOptions.Clustering), nameof(Clustering.DistributedGrainDirectoryEnabled));
        if (!builder.Configuration.GetValue<bool>(key))
        {
            return builder;
        }
#pragma warning disable ORLEANSEXP003 // Experimental in Orleans 10; opt-in only, see Clustering.DistributedGrainDirectoryEnabled.
        builder.AddDistributedGrainDirectory();
#pragma warning restore ORLEANSEXP003
        return builder;
    }
}
