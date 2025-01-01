// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Api.Namespaces;

/// <summary>
/// Represents the API for working with namespaces.
/// </summary>
/// <param name="namespaces"><see cref="INamespaces"/> for working namespaces.</param>
[Route("/api/event-store/{eventStore}/namespaces")]
public class NamespaceCommands(INamespaces namespaces) : ControllerBase
{
    /// <summary>
    /// Ensure a namespace exists.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="command">Command for ensuring.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost]
    public Task EnsureNamespace(
        [FromRoute] string eventStore,
        [FromBody] Ensure command) =>
        namespaces.Ensure(new() { EventStore = eventStore, Name = command.Namespace });
}
