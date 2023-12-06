// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Exception that gets thrown when an unknown <see cref="JobType"/> is encountered.
/// </summary>
public class UnknownClrTypeForJobStepType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownClrTypeForJobStepType"/> class.
    /// </summary>
    /// <param name="type"><see cref="JobType"/> that has an invalid type identifier.</param>
    public UnknownClrTypeForJobStepType(JobStepType type)
        : base($"Unknown job step type '{type}'")
    {
    }
}
