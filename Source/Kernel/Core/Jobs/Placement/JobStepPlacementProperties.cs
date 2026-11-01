// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using Orleans.Metadata;

namespace Cratis.Chronicle.Jobs.Placement;

/// <summary>
/// Applies job step placement only when the cluster opts in to Jobs role enforcement.
/// </summary>
/// <param name="options">The Chronicle configuration.</param>
public sealed class JobStepPlacementProperties(IOptions<ChronicleOptions> options) : IGrainPropertiesProvider
{
    /// <inheritdoc/>
    public void Populate(Type grainClass, GrainType grainType, Dictionary<string, string> properties)
    {
        if (options.Value.Clustering.EnforceJobsRole)
        {
            new Cratis.Orleans.Jobs.Placement.JobStepsPlacementProperties().Populate(grainClass, grainType, properties);
        }
    }
}
