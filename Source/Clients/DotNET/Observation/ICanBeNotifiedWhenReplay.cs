// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a contract for receiving notifications when a replay begins and ends.
/// </summary>
public interface ICanBeNotifiedWhenReplay
{
    /// <summary>
    /// Called when a replay begins.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task BeginReplay();

    /// <summary>
    /// Called when a replay ends.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task EndReplay();
}
