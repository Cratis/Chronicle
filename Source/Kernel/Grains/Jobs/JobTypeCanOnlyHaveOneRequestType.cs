// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Exception that gets thrown when a job has multiple <see cref="IJobRequest"/> types.
/// </summary>
/// <param name="jobType">The job type.</param>
/// <param name="jobClrType">The job clr <see cref="Type"/>.</param>
public class JobTypeCanOnlyHaveOneRequestType(JobType jobType, Type jobClrType)
    : Exception($"Job {jobType} associated with clr type {jobClrType} has multiple Job Request types");