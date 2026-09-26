// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

/// <summary>
/// Exposes the partition replay completion hook for specifications.
/// </summary>
/// <param name="replayStateServiceClient">The replay state service client.</param>
/// <param name="jsonSerializerOptions">The serializer options.</param>
/// <param name="logger">The logger.</param>
public class TestableReplayObserverPartition(
    IObserverServiceClient replayStateServiceClient,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<ReplayObserverPartition> logger)
    : ReplayObserverPartition(replayStateServiceClient, jsonSerializerOptions, logger), IGrainType
{
    /// <inheritdoc/>
    public Type GrainType => typeof(IReplayObserverPartition);

    /// <summary>
    /// Completes the partition replay with the state set by the specification.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task CompleteForTesting() => OnAllStepsCompleted();
}
