// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition;

/// <summary>
/// A testable subclass of <see cref="RetryFailedPartition"/> that returns no job steps from
/// <see cref="PrepareSteps"/>, causing <see cref="Job{TRequest,TJobState}.OnCompleted"/> to
/// fire immediately when <see cref="IJob{TRequest}.Start"/> is called.
/// </summary>
/// <param name="jsonSerializerOptions">The serializer options used for JSON serialization.</param>
/// <param name="logger">The logger.</param>
public class TestableRetryFailedPartition(JsonSerializerOptions jsonSerializerOptions, ILogger<RetryFailedPartition> logger)
    : RetryFailedPartition(jsonSerializerOptions, logger), IGrainType
{
    /// <inheritdoc/>
    public Type GrainType => typeof(IRetryFailedPartition);

    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(RetryFailedPartitionRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(ImmutableList<JobStepDetails>.Empty);
}
