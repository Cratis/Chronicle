// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Idempotency;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Imports;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Imports;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    /// <summary>
    /// The causation property carrying the import identifier.
    /// </summary>
    internal const string ImportIdProperty = "importId";

    /// <summary>
    /// The causation property carrying the chunk an imported event was committed in.
    /// </summary>
    internal const string ImportChunkIndexProperty = "chunkIndex";

    /// <summary>
    /// The causation property carrying the index of an imported event in its import.
    /// </summary>
    internal const string ImportEventIndexProperty = "eventIndex";

    /// <summary>
    /// The causation property carrying the sequence number an imported event had in its source.
    /// </summary>
    internal const string ImportSourceSequenceNumberProperty = "sourceSequenceNumber";

    /// <summary>
    /// The most imports whose whole-import validation claims one event sequence keeps at once.
    /// </summary>
    /// <remarks>
    /// The claims live in the grain's memory between validation calls. An import that starts validating while as many
    /// imports hold claims is told to wait and start again later; an import in the middle of its validation never loses
    /// its claims to a newcomer, so imports queue up instead of evicting each other. Claims that no call has touched for
    /// <see cref="ImportValidationIdleTimeout"/> belong to a validation that stopped without releasing them, and give way.
    /// </remarks>
    internal const int MaxImportsValidating = 4;

    /// <summary>
    /// How long the claims of an import may go untouched before they are taken to belong to a validation that stopped.
    /// </summary>
    /// <remarks>
    /// A running validation touches its claims with every chunk, so this is far longer than one chunk takes.
    /// </remarks>
    internal static readonly TimeSpan ImportValidationIdleTimeout = TimeSpan.FromMinutes(5);

    readonly Dictionary<ImportId, (ConstraintBatchClaims Claims, DateTimeOffset Touched)> _importClaims = [];
    bool _committingImport;

    /// <inheritdoc/>
    public async Task<ImportChunkValidationResult> ValidateImportChunk(ImportId importId, bool first, IReadOnlyList<ImportEventToAppend> events)
    {
        if (!_mayWriteSequence) throw new ReservedEventSequenceName(_eventSequenceId);
        if (first && !TryStartImportClaims(importId)) return ImportChunkValidationResult.Wait;
        if (!_importClaims.TryGetValue(importId, out var entry)) return ImportChunkValidationResult.Restart;
        var claims = entry.Claims;
        _importClaims[importId] = (claims, _timeProvider.GetUtcNow());

        await RefreshConstraintsIfChanged();
        if (await RefusedByPendingConstraintIndex(events.Select(_ => _.Event.EventType.Id).Distinct().ToArray()))
        {
            // The import's claims were read against an index that is being rebuilt; they cannot be trusted any more.
            _importClaims.Remove(importId);
            return ImportChunkValidationResult.Restart;
        }

        var correlationId = CorrelationId.New();
        var rejections = new List<ImportRejection>();
        var prepared = new List<(ImportEventToAppend Event, ConstraintValidationContext Context)>();
        foreach (var @event in events)
        {
            var (context, rejection) = await PrepareImportConstraintContext(@event, correlationId, claims);
            if (rejection is not null) rejections.Add(rejection);
            else prepared.Add((@event, context!));
        }

        await ReadPersistedConstraintClaims(prepared.ConvertAll(_ => _.Context), claims);
        foreach (var (@event, context) in prepared)
        {
            var result = await context.Validate();
            if (!result.IsValid)
            {
                rejections.Add(new(@event.Index, @event.Event.EventType, result.Violations.Select(DescribeViolation).ToArray()));
            }
        }

        return new(rejections.OrderBy(_ => _.Index).ToArray());
    }

    /// <inheritdoc/>
    public Task EndImportValidation(ImportId importId)
    {
        _importClaims.Remove(importId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ImportChunkAppendResult> AppendImportChunk(
        ImportId importId,
        int chunkIndex,
        EventSequenceNumber committedFrom,
        bool mayBeCommitted,
        IReadOnlyList<ImportEventToAppend> events)
    {
        if (!_mayWriteSequence) throw new ReservedEventSequenceName(_eventSequenceId);

        var outcomes = new Dictionary<long, ImportEventOutcome>();
        if (mayBeCommitted)
        {
            var found = await FindCommittedImportEvents(importId, committedFrom, events.Select(_ => _.Index).ToHashSet());
            var recovered = new List<AppendedEvent>();
            foreach (var @event in events.Where(_ => found.ContainsKey(_.Index)))
            {
                var stored = found[@event.Index];
                outcomes[@event.Index] = new(@event.Index, @event.Event.EventType, ImportEventOutcomeKind.Appended, stored.Context.SequenceNumber, []);
                recovered.Add(stored);
            }

            // The attempt that stored these may have stopped before handing the observers to catch-up.
            if (recovered.Count > 0) await HandImportedEventsToCatchup(recovered);
        }

        var remaining = events.Where(_ => !outcomes.ContainsKey(_.Index)).ToList();
        if (remaining.Count > 0)
        {
            var requests = remaining.ConvertAll(_ => new KeyedAppend(_.Event, _.IdempotencyKey));
            _committingImport = true;
            try
            {
                var errors = await AppendImportRequests(remaining, requests, CorrelationId.New());
                if (errors.Count > 0) return ImportChunkAppendResult.NotSettled([.. errors]);
            }
            catch (Exception ex)
            {
                return ImportChunkAppendResult.NotSettled(ex.Message);
            }
            finally
            {
                _committingImport = false;
            }

            for (var index = 0; index < remaining.Count; index++)
            {
                var outcome = ToImportOutcome(remaining[index], requests[index].Result!);
                if (outcome is null)
                {
                    return ImportChunkAppendResult.NotSettled(requests[index].Result!.Result.Errors.Select(_ => _.Value).ToArray());
                }

                outcomes[remaining[index].Index] = outcome;
            }
        }

        return new(events.Select(_ => outcomes[_.Index]).ToArray(), []);
    }

    static string DescribeViolation(ConstraintViolation violation) => $"{violation.ConstraintName.Value}: {violation.Message.Value}";

    static string[] DescribeFailure(AppendResult result) =>
        [.. result.Errors.Select(_ => _.Value), .. result.ConstraintViolations.Select(DescribeViolation)];

    static bool IsTransient(AppendResult result, Concepts.EventSequences.IdempotencyKey? key) =>
        result.Errors.Any(error =>
            error == AppendError.AppendOutcomeUnknown ||
            error == AppendError.ConstraintIndexUnavailable ||
            error == AppendError.BatchNotCommittedAfterRetry ||
            (key is not null && error == IdempotencyRefusals.Unsettled(key)));

    static ImportEventOutcome? ToImportOutcome(ImportEventToAppend @event, IdempotentAppendResult result)
    {
        if (result.IsSuccess)
        {
            var kind = result.Outcome == AppendOutcome.Committed ? ImportEventOutcomeKind.Appended : ImportEventOutcomeKind.AlreadyAppended;
            return new(@event.Index, @event.Event.EventType, kind, result.Result.SequenceNumber, []);
        }

        return IsTransient(result.Result, @event.IdempotencyKey)
            ? null
            : new(@event.Index, @event.Event.EventType, ImportEventOutcomeKind.Rejected, EventSequenceNumber.Unavailable, DescribeFailure(result.Result));
    }

    bool TryStartImportClaims(ImportId importId)
    {
        _importClaims.Remove(importId);
        var now = _timeProvider.GetUtcNow();
        foreach (var idle in _importClaims.Where(_ => now - _.Value.Touched > ImportValidationIdleTimeout).Select(_ => _.Key).ToArray())
        {
            _importClaims.Remove(idle);
        }

        if (_importClaims.Count >= MaxImportsValidating) return false;
        _importClaims[importId] = (new(), now);
        return true;
    }

    /// <summary>
    /// Hands durable events of an import to the observers they are meant for through their catch-up path, never live.
    /// </summary>
    /// <param name="appendedEvents">The imported events that are durable in the log.</param>
    /// <returns>Awaitable task.</returns>
    async Task HandImportedEventsToCatchup(List<AppendedEvent> appendedEvents)
    {
        try
        {
            await (_appendedEventsQueues?.SpillMatchingToCatchup(appendedEvents) ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            logger.FailedEnqueueingAppendedEvents(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, appendedEvents.Count, ex);
            await SpillAppendedEventsQueuesToCatchup();
        }
    }

    async Task<(ConstraintValidationContext? Context, ImportRejection? Rejection)> PrepareImportConstraintContext(
        ImportEventToAppend @event,
        CorrelationId correlationId,
        ConstraintBatchClaims claims)
    {
        var e = @event.Event;
        try
        {
            var resolved = await EventSourceResolution.Resolve(EventSourcesStorage, e.EventSource, e.EventSourceType, e.eventStreamType, correlationId);
            if (resolved.TryGetError(out var resolutionFailure)) return (null, new(@event.Index, e.EventType, DescribeFailure(resolutionFailure)));

            var schema = await EventTypesStorage.GetFor(e.EventType.Id, e.EventType.Generation);
            if (EnforceEventTypeVisibility(e.EventType, schema, correlationId) is { } visibility) return (null, new(@event.Index, e.EventType, DescribeFailure(visibility)));
            if (ValidateAgainstSchema(e.EventType, e.Content, schema, correlationId).TryGetError(out var schemaFailure)) return (null, new(@event.Index, e.EventType, DescribeFailure(schemaFailure)));

            // Validation never encrypts: constraints are established on the plaintext, as at append, and no key is made.
            var protectionSchema = await schemaCache.GetProtectionSchemaFor(_eventSequenceKey.EventStore, e.EventType.Id, e.EventType.Generation, schema.Schema);
            var plaintext = expandoObjectConverter.ToExpandoObject(e.Content, schema.Schema);
            var personalData = new ConstraintPersonalDataOfEvent(
                e.Subject?.IsSet == true ? e.Subject : new Subject(e.EventSourceId.Value),
                ConstraintPersonalData.PropertiesOf(protectionSchema));
            var context = await EstablishConstraintContext(e.EventSourceId, e.EventType, correlationId, plaintext, resolved.AsT0, e.eventStreamType, e.eventStreamId, claims, personalData);
            return context.TryGetError(out var contextFailure)
                ? (null, new(@event.Index, e.EventType, DescribeFailure(contextFailure)))
                : (context.AsT0, null);
        }
        catch (Exception ex)
        {
            return (null, new(@event.Index, e.EventType, [ex.Message]));
        }
    }

    /// <summary>
    /// Finds the events of a slice of an import that an interrupted call already stored.
    /// </summary>
    /// <param name="importId">The <see cref="ImportId"/>.</param>
    /// <param name="searchFrom">The sequence number below which none of the slice's events can be.</param>
    /// <param name="indexes">The indexes in the import of the slice's events.</param>
    /// <returns>The stored events, by their index in the import.</returns>
    /// <remarks>
    /// This runs within the grain's turn, holding every other appender to the sequence, so it reads no more than it must:
    /// from where the commit says the slice can start, through a cursor that pages the log, and only until every event
    /// of the slice is found. Only the slice's events are kept.
    /// </remarks>
    async Task<Dictionary<long, AppendedEvent>> FindCommittedImportEvents(ImportId importId, EventSequenceNumber searchFrom, HashSet<long> indexes)
    {
        var found = new Dictionary<long, AppendedEvent>();
        if (indexes.Count == 0 || !searchFrom.IsActualValue || searchFrom >= State.SequenceNumber) return found;

        var tail = await EventSequenceStorage.GetTailSequenceNumber();
        if (tail == EventSequenceNumber.Unavailable || tail < searchFrom) return found;

        var import = importId.Value.ToString("D", CultureInfo.InvariantCulture);
        using var cursor = await EventSequenceStorage.GetRange(searchFrom, tail);
        while (found.Count < indexes.Count && await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                var causation = @event.Context.Causation.FirstOrDefault(_ =>
                    _.Type == CausationType.Import &&
                    _.Properties.TryGetValue(ImportIdProperty, out var id) &&
                    string.Equals(id, import, StringComparison.OrdinalIgnoreCase));
                if (causation?.Properties.TryGetValue(ImportEventIndexProperty, out var index) == true &&
                    long.TryParse(index, NumberStyles.Integer, CultureInfo.InvariantCulture, out var eventIndex) &&
                    indexes.Contains(eventIndex))
                {
                    found[eventIndex] = @event;
                }
            }
        }

        return found;
    }

    async Task<List<string>> AppendImportRequests(List<ImportEventToAppend> events, List<KeyedAppend> requests, CorrelationId correlationId)
    {
        await SettleKeys(requests, correlationId);
        var absent = Enumerable.Range(0, requests.Count).Where(index => requests[index].Result is null).ToArray();
        if (absent.Length == 0) return [];

        await RefreshConstraintsIfChanged();
        if (await RefusedByPendingConstraintIndex(absent.Select(index => requests[index].Event.EventType.Id).Distinct().ToArray()))
        {
            return [AppendError.ConstraintIndexUnavailable.Value];
        }

        // Each event settles on its own: one that is invalid now, typically because an ordinary writer claimed a unique
        // value since the import was validated, is reported and left out, and the rest of the chunk is committed.
        var uses = absent.Select(index => requests[index].Key is null ? null : new List<ProtectedValueUse>()).ToArray();
        var candidates = await ValidateBatchEvents(absent.Select(index => requests[index].Event), correlationId, new ConstraintBatchClaims(), uses);
        var accepted = new List<KeyedAppend>();
        var acceptedEvents = new List<ImportEventToAppend>();
        var validated = new List<(EventToAppend Event, ExpandoObject CompliantEvent, JsonObject CompliantContent, ConstraintValidationContext ConstraintContext)>();
        for (var position = 0; position < absent.Length; position++)
        {
            var request = requests[absent[position]];
            if (!candidates[position].Result.IsSuccess)
            {
                request.Result = new(candidates[position].Result.AsT1);
                continue;
            }

            if (request.Key is { } key)
            {
                var fingerprint = AppendFingerprint.Compute(request.Input!, uses[position]!.ToArray());
                if (fingerprint.TryGetError(out var failure))
                {
                    var error = failure == FingerprintFailure.FingerprintKeysDiverged ? IdempotencyRefusals.Unsettled(key) : IdempotencyRefusals.Unsupported(key);
                    request.Result = new(AppendResult.Failed(correlationId, [error]));
                    continue;
                }

                request.Fingerprint = fingerprint.AsT0;
            }

            var (value, content, context) = candidates[position].Result.AsT0;
            validated.Add((candidates[position].Event, value, content, context));
            accepted.Add(request);
            acceptedEvents.Add(events[absent[position]]);
        }

        if (accepted.Count == 0) return [];

        var identities = new Dictionary<Identity, IEnumerable<IdentityId>>();
        var perEvent = new List<(CorrelationId? CorrelationId, IEnumerable<IdentityId>? CausedBy)>();
        foreach (var @event in acceptedEvents)
        {
            IEnumerable<IdentityId>? causedBy = null;
            if (@event.CausedBy is { } identity)
            {
                var distinct = identity.WithoutDuplicates();
                if (!identities.TryGetValue(distinct, out var chain))
                {
                    chain = await IdentityStorage.GetFor(distinct);
                    identities[distinct] = chain;
                }

                causedBy = chain;
            }

            perEvent.Add((@event.CorrelationId, causedBy));
        }

        var system = await IdentityStorage.GetFor(Identity.System);
        var (batch, contexts) = await BuildStorageBatch(validated, correlationId, [], system, perEvent);
        for (var index = 0; index < batch.Count; index++)
        {
            if (accepted[index].Key is { } key)
                batch[index] = batch[index] with { Identity = new EventPublication(key.Identity, accepted[index].Fingerprint!.Format()) };
        }

        await StoreKeyedBatch(accepted, batch, contexts, correlationId, new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
        return [];
    }
}
