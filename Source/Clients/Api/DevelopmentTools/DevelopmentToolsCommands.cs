// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Contracts.Host;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Chronicle.Api.DevelopmentTools;

/// <summary>
/// Represents the API for development tools that allow resetting Chronicle state.
/// </summary>
/// <remarks>Only available in Debug builds - protected by the <c>DEVELOPMENT</c> compile symbol.</remarks>
[Route("/api/development-tools")]
public class DevelopmentToolsCommands : ControllerBase
{
    readonly IDevelopmentTools _developmentTools;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevelopmentToolsCommands"/> class.
    /// </summary>
    /// <param name="developmentTools">The <see cref="IDevelopmentTools"/> service.</param>
    internal DevelopmentToolsCommands(IDevelopmentTools developmentTools)
    {
        _developmentTools = developmentTools;
    }

    /// <summary>
    /// Reset all Chronicle state: drops all databases and evicts all Orleans grain activations.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [HttpPost("reset-all")]
    public Task ResetAll() => _developmentTools.ResetAll();

    /// <summary>
    /// Reset the state for a specific event store.
    /// </summary>
    /// <param name="eventStore">The name of the event store to reset.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("reset-event-store/{eventStore}")]
    public Task ResetEventStore([FromRoute] string eventStore) =>
        _developmentTools.ResetEventStore(eventStore);
}
#endif
