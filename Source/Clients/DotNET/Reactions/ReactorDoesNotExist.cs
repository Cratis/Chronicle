// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Exception that gets thrown when an Reactor does not exist.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="ReactorDoesNotExist"/>.
/// </remarks>
/// <param name="reactorId1">The invalid <see cref="ReactorId"/>.</param>
public class ReactorDoesNotExist(ReactorId reactorId1) : Exception($"Reactor with id '{reactorId1}' does not exist")
{
    /// <summary>
    /// Throw if the Reactor does not exist.
    /// </summary>
    /// <param name="reactorId">The <see cref="ReactorId"/> of the Reactor.</param>
    /// <param name="reactor">The possible null <see cref="ReactorHandler"/> >value to check.</param>
    /// <exception cref="ReactorDoesNotExist">Thrown if the Reactor handler value is null.</exception>
    public static void ThrowIfDoesNotExist(ReactorId reactorId, ReactorHandler? reactor)
    {
        if (reactor is null) throw new ReactorDoesNotExist(reactorId);
    }
}
