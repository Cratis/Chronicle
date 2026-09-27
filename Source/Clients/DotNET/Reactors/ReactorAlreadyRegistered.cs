// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// The exception that is thrown when a reactor identifier is already registered in this client.
/// </summary>
public class ReactorAlreadyRegistered : Exception
{
    /// <summary>
    /// Initializes a duplicate registration error.
    /// </summary>
    /// <param name="id">The duplicate reactor identifier.</param>
    public ReactorAlreadyRegistered(ReactorId id) : base($"Reactor '{id}' is already registered in this client.")
    {
    }

    /// <summary>
    /// Initializes a duplicate registration error for two discovered reactor types.
    /// </summary>
    /// <param name="id">The duplicate reactor identifier.</param>
    /// <param name="first">The first reactor type.</param>
    /// <param name="second">The second reactor type.</param>
    public ReactorAlreadyRegistered(ReactorId id, Type first, Type second)
        : base($"Reactor '{id}' is declared by both '{first.FullName}' and '{second.FullName}'.")
    {
    }
}
