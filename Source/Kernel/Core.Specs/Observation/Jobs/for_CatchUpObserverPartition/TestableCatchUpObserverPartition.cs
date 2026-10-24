// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserverPartition;

/// <summary>
/// Exposes partition catch-up completion for specifications.
/// </summary>
/// <param name="jsonSerializerOptions">The serializer options.</param>
/// <param name="logger">The logger.</param>
public class TestableCatchUpObserverPartition(
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<CatchUpObserverPartition> logger)
    : CatchUpObserverPartition(jsonSerializerOptions, logger), IGrainType
{
    /// <inheritdoc/>
    public Type GrainType => typeof(ICatchUpObserverPartition);

    /// <summary>
    /// Records the result of a completed step.
    /// </summary>
    /// <param name="result">The step result.</param>
    /// <returns>Awaitable task.</returns>
    public Task RecordForTesting(JobStepResult result) => OnStepCompletedOrStopped(JobStepId.New(), result);

    /// <summary>
    /// Completes partition catch-up.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task CompleteForTesting() => OnAllStepsCompleted();
}
