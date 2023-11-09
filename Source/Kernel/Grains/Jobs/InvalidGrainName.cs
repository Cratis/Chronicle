// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Exception that gets thrown when a grain name is invalid for a job.
/// </summary>
public class InvalidGrainName : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidGrainName"/> class.
    /// </summary>
    /// <param name="grainType">Violating grain type.</param>
    public InvalidGrainName(Type grainType)
        : base($"Grain type '{grainType.Name}' is invalid. No interface for the grain was matched. It should follow the convention of `IFoo` -> `Foo`.")
    {
    }
}
