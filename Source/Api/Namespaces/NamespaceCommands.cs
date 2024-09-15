// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grains.Namespaces;

namespace Cratis.Api.Namespaces;

/// <summary>
/// Represents the API for working with namespaces.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for working with grains.</param>
[Route("/api/event-store/{eventStore}/namespaces")]
public class NamespaceCommands(IGrainFactory grainFactory) : ControllerBase
{
    /// <summary>
    /// Ensure a namespace exists.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="command">Command for ensuring.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public async Task EnsureNamespace(
        [FromRoute] EventStoreName eventStore,
        [FromBody] EnsureNamespace @command)
    {
        var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
        await namespaces.Ensure(@command.Name);
    }
}
