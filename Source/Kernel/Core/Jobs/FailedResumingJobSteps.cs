// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
namespace Cratis.Chronicle.Jobs;

/// <summary>
/// The job steps that failed resuming.
/// </summary>
/// <param name="FailedJobSteps">The job step ids.</param>
public record FailedResumingJobSteps(IEnumerable<JobStepId> FailedJobSteps);
