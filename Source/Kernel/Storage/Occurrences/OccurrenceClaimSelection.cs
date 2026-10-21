// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Selects claimable work while retaining the slot of an unsuperseded leased epoch.
/// </summary>
public static class OccurrenceClaimSelection
{
    /// <summary>
    /// Selects claims from the trigger's atomic snapshot. Reclaiming an expired epoch reuses its slot;
    /// otherwise renewing an expired but unclaimed epoch could exceed the in-flight limit.
    /// </summary>
    /// <param name="occurrences">The trigger's durable snapshot.</param>
    /// <param name="now">The kernel instant.</param>
    /// <param name="max">The maximum claims returned.</param>
    /// <param name="maxInFlight">The maximum leased occurrences for the trigger.</param>
    /// <returns>The selected occurrences in due or recorded order.</returns>
    public static IReadOnlyList<Occurrence> Select(IReadOnlyList<Occurrence> occurrences, DateTimeOffset now, int max, int maxInFlight)
    {
        var capacity = Math.Max(0, maxInFlight - occurrences.Count(occurrence => occurrence.State == OccurrenceState.Leased));
        var claimable = occurrences.Where(occurrence => IsClaimable(occurrence, now));
        var selected = new List<Occurrence>();
        foreach (var occurrence in claimable.OrderBy(occurrence => occurrence.Due ?? occurrence.RecordedAt)
            .ThenBy(occurrence => occurrence.Id.Value, StringComparer.Ordinal)
            .Where(occurrence => occurrence.State == OccurrenceState.Leased || capacity > 0)
            .Take(Math.Max(0, max)))
        {
            if (occurrence.State != OccurrenceState.Leased)
            {
                capacity--;
            }

            selected.Add(occurrence);
        }

        return selected;
    }

    static bool IsClaimable(Occurrence occurrence, DateTimeOffset now) => occurrence.State switch
    {
        OccurrenceState.Pending => !occurrence.NextAttempt.HasValue || occurrence.NextAttempt <= now,
        OccurrenceState.Leased => occurrence.LeaseExpiry <= now,
        _ => false
    };
}
