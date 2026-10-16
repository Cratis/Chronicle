// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Stages closing-constraint rebuilds before changing any persisted closure facts.
/// </summary>
/// <param name="storage">The closure storage.</param>
/// <param name="logger">The job-step logger.</param>
/// <remarks>
/// A redacted property-sourced transition can be reconstructed only from a closure snapshot at the same
/// sequence number. If none exists, this entire owner's rebuild is skipped without modifying its rows.
/// Other constraints continue rebuilding. Staging stores only the resulting scopes, not the event history.
/// Commit writes only differences. The event sequence's reindex handoff restores later live
/// transitions that these writes overwrite; each scope stays old or rebuilt on a partial failure.
/// </remarks>
internal sealed class ClosesStreamConstraintReindexer(IClosedStreamsConstraintStorage storage, ILogger<ReindexConstraintsStep> logger)
{
    readonly List<Rebuild> _rebuilds = [];

    /// <summary>
    /// Resolves the original type of a redacted closing transition.
    /// </summary>
    /// <param name="event">The redaction marker.</param>
    /// <returns>The original event type.</returns>
    /// <exception cref="RedactedClosingEventTypeUnavailable">The marker lacks its original type.</exception>
    internal static EventTypeId OriginalType(AppendedEvent @event)
    {
        var values = (IDictionary<string, object?>)@event.Content;
        values.TryGetValue("originalEventType", out var value);
        var originalType = value?.ToString();
        if (string.IsNullOrEmpty(originalType)) throw new RedactedClosingEventTypeUnavailable(@event.Context.SequenceNumber);

        return new EventTypeId(originalType);
    }

    /// <summary>
    /// Resolves the original occurrence time carried by a redaction marker.
    /// </summary>
    /// <param name="event">The redaction marker.</param>
    /// <returns>The recorded occurrence time, when available.</returns>
    internal static DateTimeOffset? OriginalOccurred(AppendedEvent @event)
    {
        var values = (IDictionary<string, object?>)@event.Content;
        if (!values.TryGetValue("occurred", out var value)) return null;
        if (value is DateTimeOffset occurred) return occurred;

        return DateTimeOffset.TryParse(value?.ToString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed) ? parsed : null;
    }

    /// <summary>
    /// Snapshot every requested owner's current closure facts without changing them.
    /// </summary>
    /// <param name="definitions">The requested declarations.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidClosingStreamScope">The declaration uses the reserved manual owner name.</exception>
    internal async Task Initialize(IEnumerable<ClosesStreamConstraintDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            if (definition.Name.Value.Length == 0) throw new InvalidClosingStreamScope(definition.Name);
            var snapshot = (await storage.GetForOwner(definition.Name.Value)).ToArray();
            _rebuilds.Add(new(definition, snapshot));
        }
    }

    /// <summary>
    /// Stage a historical closing or reopening transition in event order.
    /// </summary>
    /// <param name="event">The historical event.</param>
    /// <param name="plaintext">The compliance-released payload.</param>
    /// <exception cref="InvalidClosingStreamScope">A non-redacted transition has invalid scope values.</exception>
    /// <exception cref="RedactedClosingEventTypeUnavailable">A redacted event lacks its original type.</exception>
    internal void Include(AppendedEvent @event, ExpandoObject plaintext)
    {
        if (_rebuilds.Count == 0) return;
        var redacted = @event.Context.EventType.Id == GlobalEventTypes.Redaction;
        var eventType = redacted ? OriginalType(@event) : @event.Context.EventType.Id;
        foreach (var rebuild in _rebuilds.Where(rebuild => !rebuild.Skipped && (rebuild.Definition.EventTypeIds.Contains(eventType) || rebuild.Definition.ReopenedBy.Contains(eventType))))
        {
            var reopening = rebuild.Definition.ReopenedBy.Contains(eventType);
            var snapshot = rebuild.Snapshot.GetValueOrDefault(@event.Context.SequenceNumber) ?? [];
            if (redacted && rebuild.Definition.EventStreamIdFrom is not null)
            {
                if (snapshot.Length == 0)
                {
                    rebuild.Skipped = true;
                    rebuild.Pending.Clear();
                    logger.SkippingRedactedClosingConstraint(rebuild.Definition.Name, @event.Context.SequenceNumber);
                    continue;
                }

                foreach (var closure in snapshot)
                {
                    Apply(rebuild, closure, reopening);
                }
                continue;
            }

            var context = new ConstraintValidationContext([], @event.Context.EventSourceId, eventType, plaintext, @event.Context.EventSourceType, @event.Context.EventStreamType, @event.Context.EventStreamId);
            if (!rebuild.Definition.TryResolveScope(context, out var scope)) throw new InvalidClosingStreamScope(rebuild.Definition.Name);
            var existing = snapshot.FirstOrDefault(closure => closure.Scope == scope);
            var closureTime = existing?.ClosedAt ?? (redacted ? OriginalOccurred(@event) : @event.Context.Occurred);
            Apply(rebuild, new(scope, rebuild.Definition.Name.Value, @event.Context.SequenceNumber, closureTime), reopening);
        }
    }

    /// <summary>
    /// Write differences only for owners whose full history was reconstructible, leaving skipped owners untouched.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="beforeWrite">Called before issuing a commit write.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Commit(CancellationToken cancellationToken, Action? beforeWrite = null)
    {
        foreach (var rebuild in _rebuilds.Where(rebuild => !rebuild.Skipped))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = rebuild.Snapshot.Values.SelectMany(rows => rows).ToDictionary(closure => closure.Scope.Normalized());
            foreach (var (scope, closure) in rebuild.Pending)
            {
                if (snapshot.TryGetValue(scope, out var previous) && previous.SequenceNumber == closure.SequenceNumber && previous.ClosedAt == closure.ClosedAt) continue;
                cancellationToken.ThrowIfCancellationRequested();
                beforeWrite?.Invoke();
                await storage.Close(closure, cancellationToken);
            }
            foreach (var scope in snapshot.Keys.Where(scope => !rebuild.Pending.ContainsKey(scope)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                beforeWrite?.Invoke();
                await storage.Reopen(rebuild.Definition.Name.Value, scope, cancellationToken);
            }
        }
    }

    static void Apply(Rebuild rebuild, ClosedStream closure, bool reopening)
    {
        if (reopening)
        {
            rebuild.Pending.Remove(closure.Scope.Normalized());
        }
        else
        {
            rebuild.Pending[closure.Scope.Normalized()] = closure;
        }
    }

    sealed class Rebuild(ClosesStreamConstraintDefinition definition, ClosedStream[] snapshot)
    {
        /// <summary>
        /// Gets the requested declaration.
        /// </summary>
        internal ClosesStreamConstraintDefinition Definition { get; } = definition;

        /// <summary>
        /// Gets the snapshot indexed by its closing event position.
        /// </summary>
        internal Dictionary<EventSequenceNumber, ClosedStream[]> Snapshot { get; } = snapshot.GroupBy(closure => closure.SequenceNumber).ToDictionary(group => group.Key, group => group.ToArray());

        /// <summary>
        /// Gets the staged final closure facts.
        /// </summary>
        internal Dictionary<ClosedStreamScope, ClosedStream> Pending { get; } = [];

        /// <summary>
        /// Gets or sets whether this owner's history cannot be reconstructed.
        /// </summary>
        internal bool Skipped { get; set; }
    }
}
