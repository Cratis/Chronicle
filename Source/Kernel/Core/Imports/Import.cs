// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Imports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents an implementation of <see cref="IImport"/>.
/// </summary>
/// <param name="state">The <see cref="IPersistentState{TState}"/> holding the import.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for starting the commit job.</param>
/// <param name="storage">The <see cref="IStorage"/> holding the staged chunks.</param>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/>; the system clock when not given.</param>
[GrainType("chronicle-import")]
public class Import(
    [PersistentState(nameof(ImportGrainState), WellKnownGrainStorageProviders.Imports)] IPersistentState<ImportGrainState> state,
    IGrainFactory grainFactory,
    IStorage storage,
    IOptions<ChronicleOptions> options,
    ILogger<Import> logger,
    TimeProvider? timeProvider = null) : Grain, IImport
{
    readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    ImportKey _key = null!;

    ImportRecord? Record => state.State.Record;

    IImportStorage ImportStorage => storage.GetEventStore(_key.EventStore).GetNamespace(_key.Namespace).Imports;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = ImportKey.Parse(this.GetPrimaryKeyString());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ImportOperationResult> Begin(EventSequenceId eventSequenceId, OnInvalidEvent onInvalid)
    {
        if (Record is { } existing)
        {
            return existing.EventSequenceId == eventSequenceId && existing.OnInvalid == onInvalid
                ? new(existing)
                : ImportOperationResult.Refused(existing, $"Import '{_key.ImportId}' already exists for event sequence '{existing.EventSequenceId}' with other options.");
        }

        state.State.Record = new ImportRecord(_key.ImportId, eventSequenceId, onInvalid, _time.GetUtcNow());
        await state.WriteStateAsync();
        logger.ImportBegun(_key.ImportId, eventSequenceId);
        return new(Record);
    }

    /// <inheritdoc/>
    public Task<ImportRecord?> Get() => Task.FromResult(Record);

    /// <inheritdoc/>
    public async Task<ImportOperationResult> EnsureAcceptsChunks()
    {
        if (Record is null) return ImportOperationResult.Refused(null, $"Import '{_key.ImportId}' does not exist.");
        if (await AbandonIfExpired()) return ImportOperationResult.Refused(Record, $"Import '{_key.ImportId}' was abandoned after its staging retention ran out.");

        return Record.State.AcceptsChunks()
            ? new(Record)
            : ImportOperationResult.Refused(Record, $"Import '{_key.ImportId}' is {Record.State} and accepts no more chunks.");
    }

    /// <inheritdoc/>
    public async Task<bool> ChunkStaged(ImportChunkSummary summary)
    {
        if (Record is null) return false;
        if (!Record.State.AcceptsChunks())
        {
            // Counts are taken from staging when the import completes, so a chunk that completion counted is kept; any
            // other chunk arrived after the import closed and would never be committed or removed.
            return Record.State is ImportState.Validating or ImportState.Committing && summary.ChunkIndex < Record.ExpectedChunks;
        }

        state.State.Record = Record with { State = ImportState.Staging, LastActivity = _time.GetUtcNow() };
        await state.WriteStateAsync();
        return true;
    }

    /// <inheritdoc/>
    public async Task<ImportOperationResult> Complete(int expectedChunks, long expectedEvents)
    {
        if (Record is null) return ImportOperationResult.Refused(null, $"Import '{_key.ImportId}' does not exist.");
        if (Record.State is ImportState.Validating or ImportState.Committing or ImportState.Completed &&
            Record.ExpectedChunks == expectedChunks && Record.ExpectedEvents == expectedEvents)
        {
            return new(Record);
        }

        if (await AbandonIfExpired() || !Record.State.AcceptsChunks())
        {
            return ImportOperationResult.Refused(Record, $"Import '{_key.ImportId}' is {Record.State} and cannot be completed.");
        }

        var summaries = (await ImportStorage.GetChunkSummaries(_key.ImportId)).Where(_ => _.ChunkIndex < expectedChunks).ToArray();
        var received = summaries.Sum(_ => (long)_.EventsReceived);
        if (summaries.Length != expectedChunks || received != expectedEvents)
        {
            return await FailBeforeCommit(
                $"The import was completed with {expectedChunks} chunks and {expectedEvents} events, but staging holds {summaries.Length} of those chunks with {received} events. Nothing was committed.",
                expectedChunks,
                expectedEvents);
        }

        // The summaries carry the event types and rejection counts; only chunks with rejections are read, and only
        // until the import retains as many rejections as it keeps.
        var stagingRejections = new List<ImportRejection>();
        foreach (var summary in summaries.Where(_ => _.EventsRejected > 0))
        {
            if (stagingRejections.Count >= ImportRecord.MaxRetainedRejections) break;
            var chunk = await ImportStorage.GetChunk(_key.ImportId, summary.ChunkIndex);
            if (chunk is not null) stagingRejections.AddRange(chunk.Rejections);
        }

        var rejectedCount = summaries.Sum(_ => (long)_.EventsRejected);
        var retained = stagingRejections.OrderBy(_ => _.Index).Take(ImportRecord.MaxRetainedRejections).ToArray();
        state.State.Record = Record with
        {
            ExpectedChunks = expectedChunks,
            ExpectedEvents = expectedEvents,
            EventsStaged = summaries.Sum(_ => (long)_.EventsStaged),
            EventTypes = [.. summaries.SelectMany(_ => _.EventTypes).Distinct(StringComparer.Ordinal)],
            Rejections = retained,
            RejectedCount = rejectedCount,
            LastActivity = _time.GetUtcNow()
        };

        if (rejectedCount > 0 && Record.OnInvalid == OnInvalidEvent.Reject)
        {
            return await FailBeforeCommit($"{rejectedCount} events of the import are invalid. Nothing was committed.", expectedChunks, expectedEvents);
        }

        state.State.Record = Record with { State = ImportState.Validating };
        await state.WriteStateAsync();

        var started = await grainFactory.GetJobsManager(_key.EventStore, _key.Namespace)
            .Start<IImportEvents, ImportEventsRequest>(new(_key.ImportId));
        if (started.TryGetError(out var error))
        {
            await Fail($"The commit job could not be started: {error}");
            return ImportOperationResult.Refused(Record, Record.FailureReason!);
        }

        logger.ImportCompleting(_key.ImportId, expectedChunks, expectedEvents);
        return new(Record);
    }

    /// <inheritdoc/>
    public async Task<ImportOperationResult> Abandon(string reason)
    {
        if (Record is null) return ImportOperationResult.Refused(null, $"Import '{_key.ImportId}' does not exist.");
        if (Record.State == ImportState.Abandoned) return new(Record);
        if (Record.State is ImportState.Validating or ImportState.Committing)
        {
            return ImportOperationResult.Refused(
                Record,
                $"Import '{_key.ImportId}' is {Record.State}; only an import that is still staging can be abandoned. To give up a commit, stop and remove its commit job: the import then fails, keeping what was committed and discarding its staging.");
        }

        if (!Record.State.AcceptsChunks())
        {
            return ImportOperationResult.Refused(Record, $"Import '{_key.ImportId}' is {Record.State}; only an import that is still staging can be abandoned.");
        }

        state.State.Record = Record with { State = ImportState.Abandoned, FailureReason = reason, LastActivity = _time.GetUtcNow() };
        await state.WriteStateAsync();
        await ImportStorage.RemoveChunks(_key.ImportId);
        logger.ImportAbandoned(_key.ImportId, reason);
        return new(Record);
    }

    /// <inheritdoc/>
    public async Task<bool> AbandonIfExpired()
    {
        if (Record?.State.AcceptsChunks() != true) return false;
        if (_time.GetUtcNow() - Record.LastActivity <= options.Value.Imports.StagingRetention) return false;

        await Abandon($"The import was open for longer than the staging retention of {options.Value.Imports.StagingRetention} without activity.");
        return true;
    }

    /// <inheritdoc/>
    public async Task ValidationRejected(IEnumerable<ImportRejection> rejections)
    {
        if (Record is null || Record.State != ImportState.Validating) return;
        var all = rejections.ToArray();
        state.State.Record = Record.WithRejections(all) with
        {
            State = ImportState.Failed,
            FailureReason = $"{all.Length} events of the import would break a constraint. Nothing was committed.",
            LastActivity = _time.GetUtcNow()
        };
        await state.WriteStateAsync();
        await ImportStorage.RemoveChunks(_key.ImportId);
        logger.ImportFailed(_key.ImportId, Record.FailureReason!);
    }

    /// <inheritdoc/>
    public async Task CommitStarted(EventSequenceNumber committedFrom)
    {
        if (Record is null || Record.State != ImportState.Validating) return;
        state.State.Record = Record with { State = ImportState.Committing, CommitStartedAt = committedFrom.Value, LastActivity = _time.GetUtcNow() };
        await state.WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task ChunkCommitted(int chunkIndex, IEnumerable<ImportEventOutcome> outcomes)
    {
        if (Record is null || Record.State != ImportState.Committing || chunkIndex <= Record.LastCommittedChunk) return;
        var settled = outcomes.ToArray();
        var appended = settled.Where(_ => _.Kind == ImportEventOutcomeKind.Appended).Select(_ => _.SequenceNumber.Value).ToArray();
        var rejected = settled
            .Where(_ => _.Kind == ImportEventOutcomeKind.Rejected)
            .Select(_ => new ImportRejection(_.Index, _.EventType, _.Reasons))
            .ToArray();
        var ranges = appended.Length == 0
            ? Record.CommittedRanges
            : [.. Record.CommittedRanges, new ImportCommittedRange(chunkIndex, appended.Min(), appended.Max())];

        state.State.Record = Record.WithRejections(rejected) with
        {
            LastCommittedChunk = chunkIndex,
            EventsCommitted = Record.EventsCommitted + appended.Length,
            AlreadyAppended = Record.AlreadyAppended + settled.Count(_ => _.Kind == ImportEventOutcomeKind.AlreadyAppended),
            CommittedRanges = ranges,
            LastActivity = _time.GetUtcNow()
        };
        await state.WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task CommitCompleted()
    {
        if (Record is null || Record.State != ImportState.Committing) return;
        state.State.Record = Record with { State = ImportState.Completed, LastActivity = _time.GetUtcNow() };
        await state.WriteStateAsync();
        await ImportStorage.RemoveChunks(_key.ImportId);
        logger.ImportCompleted(_key.ImportId, Record.EventsCommitted, Record.AlreadyAppended, Record.RejectedCount);
    }

    /// <inheritdoc/>
    public async Task Fail(string reason)
    {
        if (Record?.State.IsTerminal() != false) return;
        state.State.Record = Record with { State = ImportState.Failed, FailureReason = reason, LastActivity = _time.GetUtcNow() };
        await state.WriteStateAsync();

        // A failed import is never committed again, so its staging, which may hold personal data in plaintext, goes.
        await ImportStorage.RemoveChunks(_key.ImportId);
        logger.ImportFailed(_key.ImportId, reason);
    }

    /// <inheritdoc/>
    public async Task<bool> FailIfStalled()
    {
        if (Record?.State is not (ImportState.Validating or ImportState.Committing)) return false;
        var retention = options.Value.Imports.StagingRetention;
        if (_time.GetUtcNow() - Record.LastActivity <= retention) return false;

        await Fail($"The import made no progress for longer than the staging retention of {retention}; its commit job is gone or stopped. Committed events stay committed.");
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveIfExpired()
    {
        if (Record?.State.IsTerminal() != true) return false;
        if (_time.GetUtcNow() - Record.LastActivity <= options.Value.Imports.StagingRetention) return false;

        // Clearing the state removes the record and every chunk left of it; the identifier can then be begun afresh.
        await state.ClearStateAsync();
        state.State.Record = null;
        return true;
    }

    async Task<ImportOperationResult> FailBeforeCommit(string reason, int expectedChunks, long expectedEvents)
    {
        state.State.Record = Record! with
        {
            State = ImportState.Failed,
            FailureReason = reason,
            ExpectedChunks = expectedChunks,
            ExpectedEvents = expectedEvents,
            LastActivity = _time.GetUtcNow()
        };
        await state.WriteStateAsync();
        await ImportStorage.RemoveChunks(_key.ImportId);
        logger.ImportFailed(_key.ImportId, reason);
        return ImportOperationResult.Refused(Record, reason);
    }
}
