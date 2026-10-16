// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

public partial class ConstraintIndexReplay
{
    async Task<(bool Finished, int Count)> ReadSegment(int limit, CancellationToken cancellationToken)
    {
        _position ??= _scopes.Min(scope => scope.From);
        if (_position! >= _endPoint) return (true, 0);
        var types = ReplayEventTypes();
        if (types.Length == 0) return (true, 0);

        // A cursor has no upper bound and may decode an entire page before yielding a slot.
        // On later passes, point-read the prefix through known corrupt slots before opening it.
        if (_readIndividually || (_knownBadReadEnd is { } badEnd && _position! < badEnd)) return await ReadIndividualEvents(types, limit, cancellationToken);
        _operationConstraint = null;
        _operationPosition = null;
        using var cursor = await events.GetFromSequenceNumber(_position!, eventTypes: types, cancellationToken: cancellationToken).WaitAsync(cancellationToken);
        StorageCallSucceeded = true;
        try
        {
            var count = 0;
            while (await MoveNext(cursor, cancellationToken))
            {
                foreach (var @event in cursor.Current)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (@event.Context.SequenceNumber >= _endPoint) return (true, count);
                    if (count >= limit) return (false, count);
                    if (!await IncludeEvent(@event, cancellationToken)) return (false, count);
                    _position = @event.Context.SequenceNumber.Next();
                    _operationPosition = null;
                    count++;
                }
            }
            return (true, count);
        }
        catch (Exception error) when (!cancellationToken.IsCancellationRequested && _operationPosition is null && StorageErrorClassifier?.IsTransient(error) is false)
        {
            // A page fault has no event identity. Locate individual relevant slots on the next turn.
            _readIndividually = true;
            _individualReadsRemaining = IndividualReadWindowSize;
            _individualReadFailureEnd = null;
            throw;
        }
    }

    async Task<bool> IncludeEvent(AppendedEvent @event, CancellationToken cancellationToken)
    {
        _operationConstraint = null;
        _operationPosition = @event.Context.SequenceNumber;
        var applicableScopes = _scopes.Where(scope => @event.Context.SequenceNumber >= scope.From && !IsPoison(scope.Definition.Name, @event.Context.SequenceNumber)).ToArray();
        if (applicableScopes.Length == 0) return true;
        var plaintext = await content.Release(@event, cancellationToken);
        foreach (var scope in applicableScopes)
        {
            try
            {
                await Include(scope.Definition, @event, plaintext, cancellationToken);
            }
            catch (Exception error) when (error is InvalidClosingStreamScope or RedactedClosingEventTypeUnavailable)
            {
                SettleError(scope.Definition, @event, error);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error)
            {
                Fail(scope.Definition.Name, @event.Context.SequenceNumber, error);
                return false;
            }
            if (!Fits())
            {
                Split();
                return false;
            }
        }
        return true;
    }

    async Task Include(IConstraintDefinition definition, AppendedEvent @event, ExpandoObject plaintext, CancellationToken cancellationToken)
    {
        var context = new ConstraintValidationContext([], @event.Context.EventSourceId, @event.Context.EventType.Id, plaintext, @event.Context.EventSourceType, @event.Context.EventStreamType, @event.Context.EventStreamId);
        if (definition is UniqueConstraintDefinition uniqueDefinition)
        {
            await IncludeUnique(uniqueDefinition, context, @event.Context.SequenceNumber, cancellationToken);
        }
        else if (definition is ClosesStreamConstraintDefinition closing)
        {
            await IncludeClosing(closing, @event, plaintext, cancellationToken);
        }
    }

    bool InSegment(string key) => BitConverter.ToUInt32(SHA256.HashData(Encoding.UTF8.GetBytes(key))) % (uint)_segments == (uint)_segment;

    void SetUnique(UniqueKey key, UniqueState state)
    {
        if (!InSegment($"{key.Name}\0{key.Scope}\0{key.Source}\0{key.Retained}")) return;
        if (!_unique.ContainsKey(key) && _unique.Count + _closed.Count + _differences.Count >= keyLimit)
        {
            _overflow = true;
            return;
        }
        _unique[key] = state;
        TrackKeys();
    }

    async Task IncludeUnique(UniqueConstraintDefinition definition, ConstraintValidationContext context, EventSequenceNumber position, CancellationToken cancellationToken)
    {
        var scope = definition.Scope.BuildScopeKey(context.EventSourceType, context.EventStreamType, context.EventStreamId);
        if (definition.RemovedWith.Contains(context.EventTypeId))
        {
            await IncludeUniqueRemoval(definition, context, scope, position, cancellationToken);
            return;
        }

        if (!definition.SupportsEventType(context.EventTypeId) || StoredConstraintEventContent.IsErased(definition, context)) return;
        var values = definition.GetPropertiesAndValues(context).ToArray();
        if (values.Length == 0)
        {
            if (definition.Mode == UniqueConstraintMode.PerEventSource) SetUniqueRemoved(definition, context.EventSourceId, scope, null, position);
            return;
        }
        var claim = new UniqueConstraintValue(values.GetValue(definition.IgnoreCasing));
        var retained = definition.Mode == UniqueConstraintMode.PerValue ? claim : null;
        SetUnique(new(definition.Name, context.EventSourceId, scope, retained), new(definition, claim, position));
    }

    void SetUniqueRemoved(UniqueConstraintDefinition definition, EventSourceId source, string scope, UniqueConstraintValue? retained, EventSequenceNumber position) =>
        SetUnique(new(definition.Name, source, scope, retained), new(definition, null, position));

    async Task IncludeUniqueRemoval(UniqueConstraintDefinition definition, ConstraintValidationContext context, string scope, EventSequenceNumber position, CancellationToken cancellationToken)
    {
        var removal = definition.RemovalEventDefinitions.SingleOrDefault(item => item.EventTypeId == context.EventTypeId);
        if (definition.Mode != UniqueConstraintMode.PerValue)
        {
            SetUniqueRemoved(definition, context.EventSourceId, scope, null, position);
        }
        else if (removal?.Properties.Any() == true)
        {
            var properties = removal.GetPropertiesAndValues(context).ToArray();
            if (properties.Length > 0)
            {
                SetUniqueRemoved(definition, context.EventSourceId, scope, new UniqueConstraintValue(properties.GetValue(definition.IgnoreCasing)), position);
            }
        }
        else
        {
            await IncludeRetainedValueRemoval(definition, context, scope, position, cancellationToken);
        }
    }

    async Task IncludeRetainedValueRemoval(UniqueConstraintDefinition definition, ConstraintValidationContext context, string scope, EventSequenceNumber position, CancellationToken cancellationToken)
    {
        var retainedValues = unique.GetForStream(context.EventSourceId, definition, scope, cancellationToken).GetAsyncEnumerator(cancellationToken);
        try
        {
            while (await retainedValues.MoveNextAsync().AsTask().WaitAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                SetUniqueRemoved(definition, context.EventSourceId, scope, retainedValues.Current, position);
                if (!Fits()) return;
            }
        }
        finally
        {
            await retainedValues.DisposeAsync().AsTask().WaitAsync(cancellationToken);
        }
        foreach (var key in _unique.Keys.Where(key => key.Name == definition.Name && key.Source == context.EventSourceId && key.Scope == scope).ToArray())
        {
            _unique[key] = new(definition, null, position);
        }
    }

    async Task IncludeClosing(ClosesStreamConstraintDefinition definition, AppendedEvent @event, ExpandoObject plaintext, CancellationToken cancellationToken)
    {
        var redacted = @event.Context.EventType.Id == GlobalEventTypes.Redaction;
        var type = redacted ? ClosesStreamConstraintReindexer.OriginalType(@event) : @event.Context.EventType.Id;
        var reopening = definition.ReopenedBy.Contains(type);
        if (!reopening && !definition.EventTypeIds.Contains(type)) return;
        if (definition.Name.Value.Length == 0) throw new InvalidClosingStreamScope(definition.Name);
        if (redacted && definition.EventStreamIdFrom is not null)
        {
            var snapshot = (await closed.GetForEvent(definition.Name.Value, @event.Context.SequenceNumber, cancellationToken)).ToArray();
            if (snapshot.Length == 0) throw new InvalidClosingStreamScope(definition.Name);
            foreach (var row in snapshot)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SetClosing(definition.Name, row.Scope, reopening ? null : row, @event.Context.SequenceNumber);
                if (!Fits()) return;
            }
            return;
        }

        var context = new ConstraintValidationContext([], @event.Context.EventSourceId, type, plaintext, @event.Context.EventSourceType, @event.Context.EventStreamType, @event.Context.EventStreamId);
        if (!definition.TryResolveScope(context, out var scope)) throw new InvalidClosingStreamScope(definition.Name);
        var occurred = redacted ? ClosesStreamConstraintReindexer.OriginalOccurred(@event) ?? @event.Context.Occurred : @event.Context.Occurred;
        SetClosing(definition.Name, scope, reopening ? null : new(scope, definition.Name.Value, @event.Context.SequenceNumber, occurred), @event.Context.SequenceNumber);
    }

    void SetClosing(ConstraintName name, ClosedStreamScope scope, ClosedStream? closure, EventSequenceNumber position)
    {
        var key = new ClosingKey(name, scope.Normalized());
        if (!InSegment($"{key.Name}\0{key.Scope}")) return;
        if (!_closed.ContainsKey(key) && _unique.Count + _closed.Count + _differences.Count >= keyLimit)
        {
            _overflow = true;
            return;
        }
        _closed[key] = new(closure, position);
        TrackKeys();
    }

    sealed record UniqueKey(ConstraintName Name, EventSourceId Source, string Scope, UniqueConstraintValue? Retained);
    sealed record UniqueState(UniqueConstraintDefinition Definition, UniqueConstraintValue? Value, EventSequenceNumber Position);
    sealed record ClosingKey(ConstraintName Name, ClosedStreamScope Scope);
    sealed record ClosingState(ClosedStream? Closure, EventSequenceNumber Position);
}
