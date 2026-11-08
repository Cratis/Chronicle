// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the work of validating and committing one import, resumable from the import's checkpoint.
/// </summary>
/// <param name="key">The <see cref="ImportKey"/>.</param>
/// <param name="import">The <see cref="IImport"/> holding the import's state and checkpoint.</param>
/// <param name="imports">The <see cref="IImportStorage"/> holding the staged chunks.</param>
/// <param name="getEventSequence">Gets the <see cref="IEventSequence"/> the import targets.</param>
/// <param name="commitChunkSize">How many events go to the event sequence in one call.</param>
/// <param name="maxAttempts">How many times a call that could not settle is tried before the import fails.</param>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/>.</param>
/// <param name="retryDelay">The optional delay between attempts; grows with each attempt when not given.</param>
public class ImportCommitWork(
    ImportKey key,
    IImport import,
    IImportStorage imports,
    Func<EventSequenceId, IEventSequence> getEventSequence,
    int commitChunkSize,
    int maxAttempts,
    TimeProvider? timeProvider = null,
    Func<int, TimeSpan>? retryDelay = null)
{
    readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    readonly Func<int, TimeSpan> _retryDelay = retryDelay ?? (attempt => TimeSpan.FromMilliseconds(Math.Min(10_000, 500 * attempt)));
    readonly int _commitChunkSize = Math.Max(1, commitChunkSize);

    /// <summary>
    /// Runs the work from wherever the import stands.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>; the work resumes from its checkpoint after it.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="ImportNotFound">The import does not exist.</exception>
    /// <exception cref="OperationCanceledException">The work was stopped; the import stays where it was and resumes from its checkpoint.</exception>
    public async Task Run(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = await import.Get() ?? throw new ImportNotFound(key.ImportId);
        var sequence = getEventSequence(record.EventSequenceId);

        if (record.State == ImportState.Validating)
        {
            if (record.OnInvalid == OnInvalidEvent.Reject)
            {
                List<ImportRejection> rejections;
                try
                {
                    rejections = await Validate(record, sequence, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    await import.Fail(ex.Message);
                    return;
                }

                if (rejections.Count > 0)
                {
                    await import.ValidationRejected(rejections);
                    return;
                }
            }

            var tail = await sequence.GetTailSequenceNumber();
            await import.CommitStarted(tail.IsActualValue ? tail.Next() : EventSequenceNumber.First);
            record = await import.Get() ?? throw new ImportNotFound(key.ImportId);
        }

        if (record.State != ImportState.Committing) return;

        try
        {
            if (!await Commit(record, sequence, cancellationToken)) return;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await import.Fail($"The commit stopped after chunk {(await import.Get())?.LastCommittedChunk}: {ex.Message}. Committed events stay committed.");
            return;
        }

        await import.CommitCompleted();
    }

    async Task<List<ImportRejection>> Validate(ImportRecord record, IEventSequence sequence, CancellationToken cancellationToken)
    {
        try
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var rejections = new List<ImportRejection>();
                var first = true;
                var restart = false;
                for (var chunkIndex = 0; chunkIndex < record.ExpectedChunks && !restart; chunkIndex++)
                {
                    foreach (var slice in await Slices(chunkIndex))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var result = await sequence.ValidateImportChunk(key.ImportId, first, slice);
                        first = false;
                        if (result.MustRestart)
                        {
                            restart = true;
                            break;
                        }

                        rejections.AddRange(result.Rejections);
                    }
                }

                if (!restart) return rejections;
                await Task.Delay(_retryDelay(attempt), _time, cancellationToken);
            }

            throw new ImportValidationUnavailable(key.ImportId);
        }
        finally
        {
            // The claims live in the event sequence's memory; they are released however validation ends.
            await ForgetClaims(sequence);
        }
    }

    async Task ForgetClaims(IEventSequence sequence)
    {
        try
        {
            await sequence.EndImportValidation(key.ImportId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The event sequence bounds the imports it keeps claims for, so claims that could not be released are
            // dropped when another import needs the room or the grain deactivates.
        }
    }

    async Task<bool> Commit(ImportRecord record, IEventSequence sequence, CancellationToken cancellationToken)
    {
        // The first chunk after a (re)start may already be partly in the log from an attempt that stopped before its
        // checkpoint; later chunks are only searched for when one of their calls is retried.
        var resuming = true;
        for (var chunkIndex = record.LastCommittedChunk + 1; chunkIndex < record.ExpectedChunks; chunkIndex++)
        {
            var committedFrom = record.CommittedFrom is { } from ? new EventSequenceNumber(from) : EventSequenceNumber.First;
            var outcomes = new List<ImportEventOutcome>();
            foreach (var slice in await Slices(chunkIndex))
            {
                outcomes.AddRange(await CommitSlice(sequence, chunkIndex, committedFrom, resuming, slice, cancellationToken));
            }

            resuming = false;
            await import.ChunkCommitted(chunkIndex, outcomes);
            record = await import.Get() ?? throw new ImportNotFound(key.ImportId);

            // An operator failed the import, or its retention ran out; nothing more is committed.
            if (record.State != ImportState.Committing) return false;
        }

        return true;
    }

    async Task<IEnumerable<ImportEventOutcome>> CommitSlice(
        IEventSequence sequence,
        int chunkIndex,
        EventSequenceNumber committedFrom,
        bool mayBeCommitted,
        IReadOnlyList<ImportEventToAppend> slice,
        CancellationToken cancellationToken)
    {
        IEnumerable<string> errors = [];
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await sequence.AppendImportChunk(key.ImportId, chunkIndex, committedFrom, mayBeCommitted || attempt > 1, slice);
                if (result.IsSettled) return result.Outcomes;
                errors = result.Errors;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The call may have committed before it failed, as a timed-out grain call can; the retry looks first.
                errors = [ex.Message];
            }

            if (attempt < maxAttempts) await Task.Delay(_retryDelay(attempt), _time, cancellationToken);
        }

        throw new ImportChunkNotCommitted(key.ImportId, chunkIndex, errors);
    }

    async Task<IReadOnlyList<IReadOnlyList<ImportEventToAppend>>> Slices(int chunkIndex)
    {
        var chunk = await imports.GetChunk(key.ImportId, chunkIndex) ?? throw new ImportChunkMissing(key.ImportId, chunkIndex);
        var events = StagedImportEventConverter.ToEventsToAppend(key.ImportId, chunkIndex, chunk.Events, _time.GetUtcNow());
        return [.. events.Chunk(_commitChunkSize)];
    }
}
