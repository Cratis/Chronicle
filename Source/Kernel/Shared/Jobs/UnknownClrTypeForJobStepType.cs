// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Jobs;

/// <summary>
/// Exception that gets thrown when an unknown <see cref="JobType"/> is encountered.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnknownClrTypeForJobStepType"/> class.
/// </remarks>
/// <param name="type"><see cref="JobType"/> that has an invalid type identifier.</param>
public class UnknownClrTypeForJobStepType(JobStepType type) : Exception($"Unknown job step type '{type}'")
{
}
