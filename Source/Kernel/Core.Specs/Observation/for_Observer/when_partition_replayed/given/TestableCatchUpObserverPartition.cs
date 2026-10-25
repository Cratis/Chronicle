// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed.given;

/// <summary>
/// Exposes catch-up preparation without starting background job execution.
/// </summary>
/// <param name="jsonSerializerOptions">The serializer options.</param>
/// <param name="logger">The logger.</param>
public class TestableCatchUpObserverPartition(JsonSerializerOptions jsonSerializerOptions, ILogger<CatchUpObserverPartition> logger)
    : CatchUpObserverPartition(jsonSerializerOptions, logger), IGrainType
{
    /// <inheritdoc/>
    public Type GrainType => typeof(ICatchUpObserverPartition);

    /// <summary>
    /// Prepares the real partition step requested by replay completion.
    /// </summary>
    /// <param name="request">The captured catch-up request.</param>
    /// <returns>The prepared steps.</returns>
    public Task<IImmutableList<JobStepDetails>> PrepareStepsForTesting(CatchUpObserverPartitionRequest request) => PrepareSteps(request);
}
