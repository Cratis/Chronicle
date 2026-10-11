// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.Events;

/// <summary>
/// The identity and independent registration and publication metadata of one registered CLR type.
/// </summary>
/// <param name="ClrType">The registered CLR type.</param>
/// <param name="EventType">The event identity and generation.</param>
/// <param name="EventStore">The declaring store, if specified.</param>
/// <param name="RegistrationVisibility">The visibility sent at registration.</param>
/// <param name="PublicationEligibility">Whether the current store may publish this type to its outbox.</param>
/// <param name="PublishingSequence">The destination for event-target publication.</param>
internal sealed record EventTypeDescriptor(
    Type ClrType,
    EventType EventType,
    string? EventStore,
    EventTypeVisibility RegistrationVisibility,
    bool PublicationEligibility,
    EventSequenceId PublishingSequence)
{
    /// <summary>
    /// Reads the independent metadata facets of an attributed event type.
    /// </summary>
    /// <param name="type">The event type.</param>
    /// <param name="currentEventStore">The current client's store.</param>
    /// <returns>The descriptor.</returns>
    internal static EventTypeDescriptor FromDeclaration(Type type, EventStoreName currentEventStore)
    {
        type.ValidateEventType();
        var eventStore = type.GetEventStoreName();
        var visibility = eventStore is not null || type.IsDefined(typeof(PublicAttribute), false)
            ? EventTypeVisibility.Public
            : EventTypeVisibility.Private;
        return new(
            type,
            ArtifactIdentity.EventTypeFor(type),
            eventStore,
            visibility,
            type.IsPublicEventType(currentEventStore),
            type.GetPublishingSequence());
    }
}
