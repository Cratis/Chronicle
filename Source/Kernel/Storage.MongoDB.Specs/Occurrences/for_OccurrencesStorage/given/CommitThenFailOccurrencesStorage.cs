// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences.for_OccurrencesStorage.given;

/// <summary>
/// Commits through the real provider and then fails every write acknowledgment.
/// </summary>
/// <param name="occurrences">The real queue.</param>
/// <param name="schedules">The real schedule storage.</param>
public class CommitThenFailOccurrencesStorage(IOccurrencesStorage occurrences, IScheduleStorage schedules) : IOccurrencesStorage, IScheduleStorage
{
    /// <inheritdoc/>
    public async Task<OccurrenceRecordResult> Record(Occurrence occurrence)
    {
        await occurrences.Record(occurrence);
        throw new AcknowledgmentLost();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Occurrence>> Claim(TriggerId trigger, string worker, DateTimeOffset now, TimeSpan duration, int max, int maxInFlight)
    {
        await occurrences.Claim(trigger, worker, now, duration, max, maxInFlight);
        throw new AcknowledgmentLost();
    }

    /// <inheritdoc/>
    public async Task<OccurrenceLeaseResult> Renew(LeaseToken token, DateTimeOffset expiry)
    {
        await occurrences.Renew(token, expiry);
        throw new AcknowledgmentLost();
    }

    /// <inheritdoc/>
    public async Task<OccurrenceLeaseResult> Complete(LeaseToken token)
    {
        await occurrences.Complete(token);
        throw new AcknowledgmentLost();
    }

    /// <inheritdoc/>
    public async Task<OccurrenceLeaseResult> Fail(LeaseToken token, string error, DateTimeOffset? nextAttempt)
    {
        await occurrences.Fail(token, error, nextAttempt);
        throw new AcknowledgmentLost();
    }

    /// <inheritdoc/>
    public async Task<bool> RetryQuarantined(OccurrenceId id)
    {
        await occurrences.RetryQuarantined(id);
        throw new AcknowledgmentLost();
    }

    /// <inheritdoc/>
    public async Task<bool> TryAddDefinition(ScheduleDefinition definition)
    {
        await schedules.TryAddDefinition(definition);
        throw new AcknowledgmentLost();
    }

    /// <inheritdoc/>
    public async Task<bool> TryUpdateDefinition(long expectedRevision, ScheduleDefinition definition)
    {
        await schedules.TryUpdateDefinition(expectedRevision, definition);
        throw new AcknowledgmentLost();
    }

    /// <inheritdoc/>
    public async Task<bool> TryAddCursor(ScheduleCursor cursor)
    {
        await schedules.TryAddCursor(cursor);
        throw new AcknowledgmentLost();
    }

    /// <inheritdoc/>
    public async Task RegisterRevision(ScheduleToken token, long revision)
    {
        await schedules.RegisterRevision(token, revision);
        throw new AcknowledgmentLost();
    }

    /// <inheritdoc/>
    public async Task<bool> TryCommitTurn(ScheduleTurn turn)
    {
        await schedules.TryCommitTurn(turn);
        throw new AcknowledgmentLost();
    }

    /// <inheritdoc/>
    public Task<Occurrence?> Get(OccurrenceId id) => occurrences.Get(id);

    /// <inheritdoc/>
    public Task<IEnumerable<Occurrence>> GetForTrigger(TriggerId trigger) => occurrences.GetForTrigger(trigger);

    /// <inheritdoc/>
    public Task<ScheduleDefinition?> GetDefinition(ScheduleToken token) => schedules.GetDefinition(token);

    /// <inheritdoc/>
    public Task<ScheduleCursor?> GetCursor(ScheduleToken token) => schedules.GetCursor(token);

    /// <inheritdoc/>
    public Task<IEnumerable<SkippedRange>> GetSkippedRanges(ScheduleToken token) => schedules.GetSkippedRanges(token);
}
