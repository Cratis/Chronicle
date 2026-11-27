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

    public string DescribeStateForTesting() =>
        $"Status={State.Status}; StatusChanges={string.Join(", ", State.StatusChanges.Select(_ => _.Status))}; History={JsonSerializer.Serialize(State.StatusChanges)}; Progress={JsonSerializer.Serialize(State.Progress)}";

    public Task<IImmutableList<JobStepDetails>> PrepareForTesting(CatchUpObserverPartitionRequest request) => PrepareSteps(request);

    public void RestoreForTesting(JobStateWithLastHandledEvent state) => State = state;
}
