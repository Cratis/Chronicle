// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Storage.Jobs;

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
}
