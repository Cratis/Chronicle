// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Choreography.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserverPartition;

public class TestableCatchUpObserverPartition(JsonSerializerOptions options, ILogger<CatchUpObserverPartition> logger)
    : CatchUpObserverPartition(options, logger), IGrainType
{
    public Type GrainType => typeof(ICatchUpObserverPartition);

    public Task<IImmutableList<JobStepDetails>> PrepareForTesting(CatchUpObserverPartitionRequest request) => PrepareSteps(request);
}
