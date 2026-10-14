// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences;

public partial class OccurrencesStorage : IOccurrencesStorage, IScheduleStorage
{
    /// <inheritdoc/>
    public Task<Occurrence?> Get(OccurrenceId id)
    {
        lock (_lock)
        {
            return Task.FromResult(_occurrences.GetValueOrDefault(id));
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Occurrence>> GetForTrigger(TriggerId trigger)
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<Occurrence>>(_occurrences.Values.Where(occurrence => occurrence.Trigger == trigger).ToArray());
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<Occurrence>> Claim(TriggerId trigger, string worker, DateTimeOffset now, TimeSpan duration, int max, int maxInFlight)
    {
        lock (_lock)
        {
            var occurrences = _occurrences.Values.Where(occurrence => occurrence.Trigger == trigger).ToArray();
            var candidates = OccurrenceClaimSelection.Select(occurrences, now, max, maxInFlight)
                .Select(occurrence => occurrence with { State = OccurrenceState.Leased, Epoch = occurrence.Epoch + 1, Attempts = occurrence.Attempts + 1, Owner = worker, LeaseExpiry = now.Add(duration), NextAttempt = null }).ToArray();
            foreach (var occurrence in candidates)
            {
                _occurrences[occurrence.Id] = occurrence;
            }

            return Task.FromResult<IEnumerable<Occurrence>>(candidates);
        }
    }

    /// <inheritdoc/>
    public Task<OccurrenceLeaseResult> Renew(LeaseToken token, DateTimeOffset expiry) => Transition(token, occurrence => occurrence with { LeaseExpiry = expiry });

    /// <inheritdoc/>
    public Task<OccurrenceLeaseResult> Complete(LeaseToken token)
    {
        lock (_lock)
        {
            if (_occurrences.TryGetValue(token.OccurrenceId, out var occurrence) && occurrence.Epoch == token.Epoch && occurrence.State == OccurrenceState.Completed)
            {
                return Task.FromResult(OccurrenceLeaseResult.AlreadyCompleted);
            }

            return Transition(token, occurrence => occurrence with { State = OccurrenceState.Completed, Owner = null, LeaseExpiry = null });
        }
    }

    /// <inheritdoc/>
    public Task<OccurrenceLeaseResult> Fail(LeaseToken token, string error, DateTimeOffset? nextAttempt) => Transition(
        token,
        occurrence => occurrence with { State = nextAttempt.HasValue ? OccurrenceState.Pending : OccurrenceState.Quarantined, Owner = null, LeaseExpiry = null, LastError = error, NextAttempt = nextAttempt });

    /// <inheritdoc/>
    public Task<bool> RetryQuarantined(OccurrenceId id)
    {
        lock (_lock)
        {
            if (!_occurrences.TryGetValue(id, out var occurrence) || occurrence.State != OccurrenceState.Quarantined)
            {
                return Task.FromResult(false);
            }

            _occurrences[id] = occurrence with { State = OccurrenceState.Pending, Attempts = 0, NextAttempt = null };
            return Task.FromResult(true);
        }
    }

    Task<OccurrenceLeaseResult> Transition(LeaseToken token, Func<Occurrence, Occurrence> update)
    {
        lock (_lock)
        {
            if (!_occurrences.TryGetValue(token.OccurrenceId, out var occurrence) || occurrence.Epoch != token.Epoch || occurrence.State != OccurrenceState.Leased)
            {
                return Task.FromResult(OccurrenceLeaseResult.OccurrenceLeaseLost);
            }

            _occurrences[token.OccurrenceId] = update(occurrence);
            return Task.FromResult(OccurrenceLeaseResult.Updated);
        }
    }
}
