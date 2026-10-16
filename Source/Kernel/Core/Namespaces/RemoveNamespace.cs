// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Removes a namespace from an event store, together with its observers, their reminders and the data stored for it.
/// </summary>
/// <param name="EventStore">The event store the namespace belongs to.</param>
/// <param name="Namespace">The namespace to remove.</param>
[Command]
[BelongsTo(WellKnownServices.Namespaces)]
public record RemoveNamespace(EventStoreName EventStore, EventStoreNamespaceName Namespace)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="remover">The <see cref="INamespaceRemover"/> that performs the removal.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(INamespaceRemover remover) => remover.Remove(EventStore, Namespace);
}
