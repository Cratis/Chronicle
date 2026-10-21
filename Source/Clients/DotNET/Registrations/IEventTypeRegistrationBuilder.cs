// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Defines the supplied declaration facets of an explicitly registered event type.
/// </summary>
public interface IEventTypeRegistrationBuilder
{
    /// <summary>
    /// Declares the stable event type identifier.
    /// </summary>
    /// <param name="id">The event identifier.</param>
    /// <returns>The builder for continuation.</returns>
    IEventTypeRegistrationBuilder WithId(EventTypeId id);

    /// <summary>
    /// Declares the event generation.
    /// </summary>
    /// <param name="generation">The generation.</param>
    /// <returns>The builder for continuation.</returns>
    IEventTypeRegistrationBuilder Generation(EventTypeGeneration generation);

    /// <summary>
    /// Makes the event publicly eligible for publication and registration.
    /// </summary>
    /// <returns>The builder for continuation.</returns>
    IEventTypeRegistrationBuilder Public();

    /// <summary>
    /// Declares private registration visibility without removing publication eligibility.
    /// </summary>
    /// <returns>The builder for continuation.</returns>
    IEventTypeRegistrationBuilder Private();

    /// <summary>
    /// Declares the event's owning store.
    /// </summary>
    /// <param name="eventStore">The owning store.</param>
    /// <returns>The builder for continuation.</returns>
    IEventTypeRegistrationBuilder ForEventStore(EventStoreName eventStore);

    /// <summary>
    /// Declares the destination for event-target publication.
    /// </summary>
    /// <param name="sequence">The destination sequence.</param>
    /// <returns>The builder for continuation.</returns>
    IEventTypeRegistrationBuilder PublishTo(EventSequenceId sequence);
}
