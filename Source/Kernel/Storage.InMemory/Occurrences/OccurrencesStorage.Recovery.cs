// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences;

public partial class OccurrencesStorage
{
    /// <inheritdoc/>
    public Task<bool> RetryQuarantined(OccurrenceId id, string principal, DateTimeOffset now)
    {
        lock (_lock)
        {
            if (!_occurrences.TryGetValue(id, out var occurrence) || occurrence.State != OccurrenceState.Quarantined)
            {
                return Task.FromResult(false);
            }

            _occurrences[id] = occurrence with { State = OccurrenceState.Pending, Attempts = 0, NextAttempt = null, RetriedBy = principal, RetriedAt = now };
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task ExpireLeases(string worker, DateTimeOffset now)
    {
        lock (_lock)
        {
            foreach (var occurrence in _occurrences.Values.Where(occurrence => occurrence.State == OccurrenceState.Leased && occurrence.Owner == worker && occurrence.LeaseExpiry > now).ToArray())
            {
                _occurrences[occurrence.Id] = occurrence with { LeaseExpiry = now };
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TriggerId>> GetPendingTriggers()
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<TriggerId>>(_occurrences.Values.Where(occurrence => occurrence.State is OccurrenceState.Pending or OccurrenceState.Leased).Select(occurrence => occurrence.Trigger).Distinct().ToArray());
        }
    }
}
