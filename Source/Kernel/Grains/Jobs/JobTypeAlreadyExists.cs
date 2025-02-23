// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Exception that gets thrown when a <see cref="JobType"/> is associated with multiple <see cref="Type"/> clr types.
/// </summary>
/// <param name="jobType">The job type.</param>
/// <param name="existingClrType">The already associated clr type.</param>
/// <param name="newClrType">The other clr type.</param>
public class JobTypeAlreadyExists(JobType jobType, Type existingClrType, Type newClrType)
    : Exception($"JobType {jobType} is already associated with {existingClrType} so it cannot be associated with {newClrType}");