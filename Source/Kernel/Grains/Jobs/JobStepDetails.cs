// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Jobs;

namespace Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents a <see cref="IJobStep"/> and the request associated with it.
/// </summary>
/// <param name="Type">Type of job step to create.</param>
/// <param name="Id">The unique identifier of the job step.</param>
/// <param name="Key">The key extension for the job step.</param>
/// <param name="Request">The request associated with the job step.</param>
/// <param name="ResultType">The result type for the job step.</param>
public record JobStepDetails(
    Type Type,
    JobStepId Id,
    JobStepKey Key,
    object Request,
    Type ResultType);
