// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Storage.Jobs;

/// <summary>
/// Exception that gets thrown when a type is not a <see cref="JobState"/>.
/// </summary>
public class InvalidJobStateType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidJobStateType"/> class.
    /// </summary>
    /// <param name="type">Type that is invalid.</param>
    public InvalidJobStateType(Type type) : base($"Type '{type.FullName}' is not a JobState")
    {
    }

    /// <summary>
    /// Throw if the type does not derive from <see cref="JobState"/>.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <exception cref="InvalidJobStateType">Thrown if type is invalid.</exception>
    public static void ThrowIfTypeDoesNotDeriveFromJobState(Type type)
    {
        if (!typeof(JobState).IsAssignableFrom(type))
        {
            throw new InvalidJobStateType(type);
        }
    }
}
