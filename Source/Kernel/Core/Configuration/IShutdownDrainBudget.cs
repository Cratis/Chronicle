// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Defines one shared deadline for draining work on this kernel.
/// </summary>
public interface IShutdownDrainBudget
{
    /// <summary>
    /// Gets the token canceled at the shared deadline.
    /// </summary>
    CancellationToken Token { get; }

    /// <summary>
    /// Gets whether kernel shutdown has begun.
    /// </summary>
    bool IsStopping { get; }

    /// <summary>
    /// Gets the effective timeout for a drain outside kernel shutdown.
    /// </summary>
    TimeSpan Timeout { get; }

    /// <summary>
    /// Starts the deadline once, without extending it for subsequent callers.
    /// </summary>
    /// <returns>The shared deadline token.</returns>
    CancellationToken Begin();
}
