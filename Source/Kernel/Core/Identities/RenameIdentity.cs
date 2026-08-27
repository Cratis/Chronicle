// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Identities;

/// <summary>
/// Represents the command for renaming the display name of an identity.
/// </summary>
/// <param name="EventStore">The name of the event store the identity belongs to.</param>
/// <param name="Namespace">The namespace the identity belongs to.</param>
/// <param name="Subject">The subject that uniquely identifies the identity to rename.</param>
/// <param name="Name">The new display name.</param>
[Command]
[BelongsTo(WellKnownServices.Identities)]
public record RenameIdentity(EventStoreName EventStore, EventStoreNamespaceName Namespace, Subject Subject, string Name)
{
    /// <summary>
    /// Handles the command by renaming the identity in the identity store.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the identities.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Only the display name changes - the subject, username and on-behalf-of chain are preserved. Chronicle keys
    /// PII encryption on the compliance subject, never on the display name, so no encryption key lookup moves.
    /// </remarks>
    internal Task Handle(IStorage storage) =>
        storage.GetEventStore(EventStore).GetNamespace(Namespace).Identities.Rename(Subject, Name);
}
