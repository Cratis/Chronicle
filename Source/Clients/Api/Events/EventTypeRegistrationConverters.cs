// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Converters between <see cref="EventTypeRegistration"/> and its contract representation.
/// </summary>
internal static class EventTypeRegistrationConverters
{
    /// <summary>
    /// Converts an <see cref="EventTypeRegistration"/> to a <see cref="Contracts.Events.EventTypeRegistration"/>.
    /// </summary>
    /// <param name="registration">The event type to convert.</param>
    /// <returns>The converted event type.</returns>
    public static Contracts.Events.EventTypeRegistration ToContract(this EventTypeRegistration registration) =>
        new()
        {
            Type = registration.Type.ToContract(),
            Owner = registration.Owner,
            Source = registration.Source,
            Schema = registration.Schema
        };

    /// <summary>
    /// Converts a collection of <see cref="EventTypeRegistration"/> to a <see cref="Contracts.Events.EventTypeRegistration"/>.
    /// </summary>
    /// <param name="registrations">The collection of registrations to convert.</param>
    /// <returns>The converted collection of event types.</returns>
    public static IEnumerable<Contracts.Events.EventTypeRegistration> ToContract(this IEnumerable<EventTypeRegistration> registrations) =>
        registrations.Select(ToContract).ToArray();

    /// <summary>
    /// Converts a <see cref="Contracts.Events.EventTypeRegistration"/> to an <see cref="EventTypeRegistration"/>.
    /// </summary>
    /// <param name="registration">The registration to convert.</param>
    /// <returns>The converted event type.</returns>
    public static EventTypeRegistration ToApi(this Contracts.Events.EventTypeRegistration registration) =>
        new(registration.Type.ToApi(), registration.Owner, registration.Source, registration.Schema);

    /// <summary>
    /// Converts a collection of <see cref="Contracts.Events.EventTypeRegistration"/> to an <see cref="EventTypeRegistration"/>.
    /// </summary>
    /// <param name="registrations">The collection of event types to convert.</param>
    /// <returns>The converted collection of event types.</returns>
    public static IEnumerable<EventTypeRegistration> ToApi(this IEnumerable<Contracts.Events.EventTypeRegistration> registrations) =>
        registrations.Select(ToApi).ToArray();
}
