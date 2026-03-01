// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Sys;

/// <summary>
/// Defines the system grain.
/// </summary>
public interface ISystem : IGrainWithIntegerKey
{
    /// <summary>
    /// Gets the current system version.
    /// </summary>
    /// <returns>The current <see cref="SemanticVersion"/> or null if not set.</returns>
    Task<SemanticVersion?> GetVersion();

    /// <summary>
    /// Sets the current system version.
    /// </summary>
    /// <param name="version">The <see cref="SemanticVersion"/> to set.</param>
    /// <returns>Awaitable task.</returns>
    Task SetVersion(SemanticVersion version);
}
