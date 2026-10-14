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
        var types = _scopes.SelectMany(scope => scope.Definition switch
        {
            UniqueConstraintDefinition unique => unique.EventDefinitions.Select(item => item.EventTypeId).Concat(unique.RemovedWith),
            ClosesStreamConstraintDefinition closing => closing.EventTypeIds.Concat(closing.ReopenedBy).Append(GlobalEventTypes.Redaction),
            _ => []
        }).Distinct().Select(id => new EventType(id, EventTypeGeneration.First)).ToArray();
        if (types.Length == 0) return (true, 0);
        _operationConstraint = null;
        _operationPosition = null;
        using var cursor = await events.GetFromSequenceNumber(_position!, eventTypes: types, cancellationToken: cancellationToken);
        var count = 0;
        while (await MoveNext(cursor, cancellationToken))
        {
            foreach (var @event in cursor.Current)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (@event.Context.SequenceNumber >= endPoint) return (true, count);
                if (count >= limit) return (false, count);
                _operationConstraint = null;
                _operationPosition = @event.Context.SequenceNumber;
                var plaintext = await content.Release(@event, cancellationToken);
                foreach (var scope in _scopes.Where(scope => @event.Context.SequenceNumber >= scope.From))
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
                        return (false, count);
                    }
                    if (!Fits())
                    {
                        Split();
                        return (false, count);
                    }
                }
                _position = @event.Context.SequenceNumber.Next();
                count++;
            }
        }

        return (true, count);
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

    void SetUnique(UniqueConstraintDefinition definition, EventSourceId source, string scope, UniqueConstraintValue? retained, UniqueConstraintValue? value, EventSequenceNumber position)
    {
        var key = new UniqueKey(definition.Name, source, scope, retained);
        if (!InSegment($"{key.Name}\0{key.Scope}\0{key.Source}\0{key.Retained}")) return;
        if (!_unique.ContainsKey(key) && _unique.Count + _closed.Count + _differences.Count >= keyLimit)
        {
            _overflow = true;
            return;
        }
        _unique[key] = new(definition, value, position);
        TrackKeys();
    }

    async Task IncludeUnique(UniqueConstraintDefinition definition, ConstraintValidationContext context, EventSequenceNumber position, CancellationToken cancellationToken)
    {
        var scope = definition.Scope.BuildScopeKey(context.EventSourceType, context.EventStreamType, context.EventStreamId);
        if (definition.RemovedWith.Contains(context.EventTypeId))
        {
            var removal = definition.RemovalEventDefinitions.SingleOrDefault(item => item.EventTypeId == context.EventTypeId);
            if (definition.Mode == UniqueConstraintMode.PerValue && removal?.Properties.Any() == true)
            {
                var properties = removal.GetPropertiesAndValues(context).ToArray();
                if (properties.Length > 0)
                {
                    var value = new UniqueConstraintValue(properties.GetValue(definition.IgnoreCasing));
                    SetUnique(definition, context.EventSourceId, scope, value, null, position);
                }
            }
            else if (definition.Mode == UniqueConstraintMode.PerValue)
            {
                await foreach (var value in unique.GetForStream(context.EventSourceId, definition, scope, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SetUnique(definition, context.EventSourceId, scope, value, null, position);
                    if (!Fits()) return;
                }
                foreach (var key in _unique.Keys.Where(key => key.Name == definition.Name && key.Source == context.EventSourceId && key.Scope == scope).ToArray())
                {
                    _unique[key] = new(definition, null, position);
                }
            }
            else
            {
                SetUnique(definition, context.EventSourceId, scope, null, null, position);
            }
            return;
        }

        if (!definition.SupportsEventType(context.EventTypeId) || StoredConstraintEventContent.IsErased(definition, context)) return;
        var values = definition.GetPropertiesAndValues(context).ToArray();
        if (values.Length == 0)
        {
            if (definition.Mode == UniqueConstraintMode.PerEventSource) SetUnique(definition, context.EventSourceId, scope, null, null, position);
            return;
        }
        var claim = new UniqueConstraintValue(values.GetValue(definition.IgnoreCasing));
        SetUnique(definition, context.EventSourceId, scope, definition.Mode == UniqueConstraintMode.PerValue ? claim : null, claim, position);
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
