// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

/// <summary>
/// Exposes the partition replay completion hook for specifications.
/// </summary>
/// <param name="replayStateServiceClient">The replay state service client.</param>
/// <param name="storage">The storage.</param>
/// <param name="jsonSerializerOptions">The serializer options.</param>
/// <param name="logger">The logger.</param>
public class TestableReplayObserverPartition(
    IObserverServiceClient replayStateServiceClient,
    Cratis.Chronicle.Storage.IStorage storage,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<ReplayObserverPartition> logger)
    : ReplayObserverPartition(replayStateServiceClient, storage, jsonSerializerOptions, logger), IGrainType
{
    /// <inheritdoc/>
    public Type GrainType => typeof(IReplayObserverPartition);

    /// <summary>
    /// Prepares partition replay steps without starting the job framework.
    /// </summary>
    /// <param name="request">The replay request.</param>
    /// <returns>The prepared steps.</returns>
    public Task<IImmutableList<JobStepDetails>> PrepareStepsForTesting(ReplayObserverPartitionRequest request) => PrepareSteps(request);

    /// <summary>
    /// Revalidates persisted replay admission before resuming job steps.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task ResumeForTesting() => OnBeforeResumingJobSteps();

    /// <summary>
    /// Completes the partition replay with the state set by the specification.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task CompleteForTesting() => OnAllStepsCompleted();
}
