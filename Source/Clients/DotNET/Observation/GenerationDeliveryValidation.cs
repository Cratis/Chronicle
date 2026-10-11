// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Validates generation pins shared by client observer registrations.
/// </summary>
internal static class GenerationDeliveryValidation
{
    /// <summary>
    /// Rejects ambiguous pins.
    /// </summary>
    /// <param name="eventTypes">The observer's event types.</param>
    /// <exception cref="MultipleGenerationsOfEventTypeInObserver">Several generations of one type are pinned.</exception>
    internal static void Validate(IEnumerable<EventType> eventTypes)
    {
        var duplicate = eventTypes.GroupBy(_ => _.Id).FirstOrDefault(_ => _.Select(type => type.Generation).Distinct().Count() > 1);
        if (duplicate is not null)
        {
            throw new MultipleGenerationsOfEventTypeInObserver(duplicate.Key);
        }
    }
}
