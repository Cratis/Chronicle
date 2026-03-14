// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
namespace Cratis.Chronicle.Contracts.Host;

/// <summary>
/// Defines development-only tools for resetting Chronicle state.
/// </summary>
/// <remarks>
/// Only available in Debug builds. Use the <c>/api/development-tools/is-available</c> endpoint
/// to check whether the server exposes these capabilities.
/// </remarks>
public interface IDevelopmentTools
{
    /// <summary>
    /// Reset all Chronicle state: drops all databases and evicts all Orleans grain activations.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task ResetAll();

    /// <summary>
    /// Reset the state for a specific event store: drops its databases, removes its registry entry,
    /// and evicts all Orleans grain activations.
    /// </summary>
    /// <param name="eventStore">The name of the event store to reset.</param>
    /// <returns>Awaitable task.</returns>
    Task ResetEventStore(string eventStore);
}
#endif
