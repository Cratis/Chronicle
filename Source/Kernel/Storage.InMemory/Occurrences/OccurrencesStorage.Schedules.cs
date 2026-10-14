// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences;

public partial class OccurrencesStorage
{
    readonly Dictionary<OccurrenceId, ScheduleToken> _materializedBy = [];
    readonly Dictionary<ScheduleToken, ScheduleDefinition> _definitions = [];
    readonly Dictionary<ScheduleToken, ScheduleCursor> _cursors = [];
    readonly Dictionary<string, SkippedRange> _skipped = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets whether any durable infrastructure facts exist in this process scope.
    /// </summary>
    public bool HasData
    {
        get
        {
            lock (_lock)
            {
                return _occurrences.Count > 0 || _cursors.Count > 0 || _definitions.Count > 0;
            }
        }
    }

    /// <inheritdoc/>
    public Task<ScheduleDefinition?> GetDefinition(ScheduleToken token)
    {
        lock (_lock)
        {
            return Task.FromResult(_definitions.GetValueOrDefault(token));
        }
    }

    /// <inheritdoc/>
    public Task<bool> TryAddDefinition(ScheduleDefinition definition)
    {
        lock (_lock)
        {
            return Task.FromResult(_definitions.TryAdd(definition.Token, definition));
        }
    }

    /// <inheritdoc/>
    public Task<bool> TryUpdateDefinition(long expectedRevision, ScheduleDefinition definition)
    {
        lock (_lock)
        {
            if (!_definitions.TryGetValue(definition.Token, out var existing) || existing.Revision != expectedRevision)
            {
                return Task.FromResult(false);
            }

            _definitions[definition.Token] = definition;
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<ScheduleCursor?> GetCursor(ScheduleToken token)
    {
        lock (_lock)
        {
            return Task.FromResult(_cursors.GetValueOrDefault(token));
        }
    }

    /// <inheritdoc/>
    public Task<bool> TryAddCursor(ScheduleCursor cursor)
    {
        lock (_lock)
        {
            return Task.FromResult(_cursors.TryAdd(cursor.Token, cursor));
        }
    }

    /// <inheritdoc/>
    public Task RegisterRevision(ScheduleToken token, long revision)
    {
        lock (_lock)
        {
            if (_cursors.TryGetValue(token, out var cursor) && cursor.RegisteredRevision < revision)
            {
                _cursors[token] = cursor with { RegisteredRevision = revision, Version = cursor.Version + 1 };
            }

            return Task.CompletedTask;
        }
    }

    /// <inheritdoc/>
    public Task<bool> TryCommitTurn(ScheduleTurn turn)
    {
        lock (_lock)
        {
            if (!_cursors.TryGetValue(turn.Observed.Token, out var current) || current != turn.Observed || turn.Next.Token != current.Token || turn.Next.RegisteredRevision != current.RegisteredRevision || turn.Next.AppliedRevision != current.RegisteredRevision || turn.Next.Version != current.Version + 1)
            {
                return Task.FromResult(false);
            }

            foreach (var occurrence in turn.Occurrences)
            {
                if (_occurrences.TryAdd(occurrence.Id, occurrence))
                {
                    _materializedBy.Add(occurrence.Id, current.Token);
                }
            }

            foreach (var range in turn.SkippedRanges)
            {
                _skipped.TryAdd(range.Id, range);
            }

            if (turn.DiscardPendingBeforeRevision.HasValue)
            {
                foreach (var occurrence in _occurrences.Values.Where(occurrence => occurrence.State == OccurrenceState.Pending && occurrence.Kind == OccurrenceKind.Schedule && _materializedBy.GetValueOrDefault(occurrence.Id) == current.Token && occurrence.DefinitionRevision < turn.DiscardPendingBeforeRevision).ToArray())
                {
                    _occurrences[occurrence.Id] = occurrence with { State = OccurrenceState.Skipped };
                }
            }

            _cursors[current.Token] = turn.Next;
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<SkippedRange>> GetSkippedRanges(ScheduleToken token)
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<SkippedRange>>(_skipped.Values.Where(range => range.Token == token).ToArray());
        }
    }
}
