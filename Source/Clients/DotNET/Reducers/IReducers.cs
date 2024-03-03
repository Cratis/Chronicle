// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Reducers;

/// <summary>
/// Defines a system for working with reducer registrations for the Kernel.
/// </summary>
public interface IReducers
{
    /// <summary>
    /// Discover all reducers from the entry assembly and dependencies.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Discover();
}
