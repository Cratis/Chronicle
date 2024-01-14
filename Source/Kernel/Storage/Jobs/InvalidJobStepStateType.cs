// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Storage.Jobs;

/// <summary>
/// Exception that gets thrown when a type is not a <see cref="JobState"/>.
/// </summary>
public class InvalidJobStepStateType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidJobStepStateType"/> class.
    /// </summary>
    /// <param name="type">Type that is invalid.</param>
    public InvalidJobStepStateType(Type type) : base($"Type '{type.FullName}' is not a JobStepState")
    {
    }

    /// <summary>
    /// Throw if the type does not derive from <see cref="JobStepState"/>.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <exception cref="InvalidJobStepStateType">Thrown if type is invalid.</exception>
    public static void ThrowIfTypeDoesNotDeriveFromJobState(Type type)
    {
        if (!typeof(JobStepState).IsAssignableFrom(type))
        {
            throw new InvalidJobStepStateType(type);
        }
    }
}
