// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.Events;

/// <summary>
/// The identity and independent registration and publication metadata of one registered CLR type.
/// </summary>
internal sealed record EventTypeDescriptor
{
    /// <summary>
    /// Gets the registered CLR type.
    /// </summary>
    internal required Type ClrType { get; init; }

    /// <summary>
    /// Gets the event identity and generation.
    /// </summary>
    internal required EventType EventType { get; init; }

    /// <summary>
    /// Gets the declaring store, if specified.
    /// </summary>
    internal required string? EventStore { get; init; }

    /// <summary>
    /// Gets the visibility sent at registration.
    /// </summary>
    internal required EventTypeVisibility RegistrationVisibility { get; init; }

    /// <summary>
    /// Gets whether the current store may publish this type to its outbox.
    /// </summary>
    internal required bool PublicationEligibility { get; init; }

    /// <summary>
    /// Gets the destination for event-target publication.
    /// </summary>
    internal required EventSequenceId PublishingSequence { get; init; }

    /// <summary>
    /// Reads the independent metadata facets of an attributed event type.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="currentEventStore">The current client's store.</param>
    /// <returns>The descriptor.</returns>
    internal static EventTypeDescriptor FromDeclaration(Type type, EventStoreName currentEventStore)
    {
        type.ValidateEventType();
        return FromRegisteredType(type, ArtifactIdentity.EventTypeFor(type), currentEventStore);
    }

    /// <summary>
    /// Reads available declaration metadata without requiring a marker for a registered type.
    /// </summary>
    /// <param name="type">The registered CLR type.</param>
    /// <param name="eventType">The registry's identity.</param>
    /// <param name="currentEventStore">The current client's store.</param>
    /// <returns>The descriptor.</returns>
    internal static EventTypeDescriptor FromRegisteredType(Type type, EventType eventType, EventStoreName currentEventStore)
    {
        var eventStore = EventTypeDeclarations.ResolveEventStore(type, candidate => candidate.GetEventStoreName());
        var visibility = eventStore is not null || type.IsDefined(typeof(PublicAttribute), false)
            ? EventTypeVisibility.Public
            : EventTypeVisibility.Private;
        return new()
        {
            ClrType = type,
            EventType = eventType,
            EventStore = eventStore,
            RegistrationVisibility = visibility,
            PublicationEligibility = type.IsPublicEventType(currentEventStore) || eventStore == currentEventStore.Value,
            PublishingSequence = type.GetPublishingSequence()
        };
    }
}
