// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines a system to work with <see cref="IConstraints">constraints</see>.
/// </summary>
public interface IConstraints
{
    /// <summary>
    /// Discover all constraints in the system.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Discover();

    /// <summary>
    /// Register all constraints with the Chronicle.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Register();
}
