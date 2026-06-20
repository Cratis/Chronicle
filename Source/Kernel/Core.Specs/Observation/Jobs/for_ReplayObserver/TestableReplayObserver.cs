// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

/// <summary>
/// A testable subclass of <see cref="ReplayObserver"/> that exposes step preparation.
/// </summary>
/// <param name="replayStateServiceClient">The <see cref="IObserverServiceClient"/> for replay notifications.</param>
/// <param name="storage">The <see cref="IStorage"/> for accessing key indexes.</param>
/// <param name="jsonSerializerOptions">The serializer options used for JSON serialization.</param>
/// <param name="logger">The logger.</param>
public class TestableReplayObserver(
    IObserverServiceClient replayStateServiceClient,
    IStorage storage,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<ReplayObserver> logger)
    : ReplayObserver(replayStateServiceClient, storage, jsonSerializerOptions, logger), IGrainType
{
    /// <inheritdoc/>
    public Type GrainType => typeof(IReplayObserver);

    /// <summary>
    /// Invokes <see cref="ReplayObserver.PrepareSteps"/> for testing.
    /// </summary>
    /// <param name="request">The request to prepare steps for.</param>
    /// <returns>The prepared steps.</returns>
    public Task<IImmutableList<JobStepDetails>> PrepareStepsForTesting(ReplayObserverRequest request) =>
        PrepareSteps(request);
}

