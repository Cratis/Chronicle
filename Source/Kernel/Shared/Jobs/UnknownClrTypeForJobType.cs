// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Jobs;

/// <summary>
/// Exception that gets thrown when an unknown <see cref="JobType"/> is encountered.
/// </summary>
public class UnknownClrTypeForJobType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownClrTypeForJobType"/> class.
    /// </summary>
    /// <param name="type"><see cref="JobType"/> that has an invalid type identifier.</param>
    public UnknownClrTypeForJobType(JobType type)
        : base($"Unknown job type '{type}'")
    {
    }
}
