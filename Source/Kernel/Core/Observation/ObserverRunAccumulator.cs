// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>Accumulates independent run-wide conditions and the ordered cursor suffix.</summary>
internal struct ObserverRunAccumulator
{
    readonly ObserverRunPreparationContext _context;
    ObserverRunBuffer _retained;
    AppendedEvent? _last;
    bool _subscribed;
    bool _sourceMatched;
    bool _streamMatched;
    bool _tagsMatched;
    bool _applicable;

    /// <summary>Initializes the independent applicability flags without capturing a predicate.</summary>
    /// <param name="context">The observer's stable preparation inputs.</param>
    /// <param name="hasSubscribedEvent">Whether the array subscription probe already established membership.</param>
    internal ObserverRunAccumulator(in ObserverRunPreparationContext context, bool hasSubscribedEvent = false)
    {
        this = default;
        _context = context;
        _subscribed = context.SubscribesToAllEvents || hasSubscribedEvent;
        _sourceMatched = SourceFilterIsDisabled(context.Filters);
        _streamMatched = StreamFilterIsDisabled(context.Filters);
        _tagsMatched = context.Filters?.Tags.Any() != true;
        _applicable = AllDimensionsMatch();
    }

    /// <summary>Updates the tail and applicability flags and retains a cursor-eligible event.</summary>
    /// <param name="appendedEvent">The next original event in input order.</param>
    internal void Observe(AppendedEvent appendedEvent)
    {
        _last = appendedEvent;
        if (!_applicable)
        {
            ObserveApplicability(appendedEvent);
        }

        if (appendedEvent.Context.SequenceNumber >= _context.NextEventSequenceNumber)
        {
            _retained.Add(appendedEvent);
        }
    }

    /// <summary>Produces the run-wide decision and the ordered delivery suffix.</summary>
    /// <returns>The preparation result.</returns>
    /// <exception cref="InvalidOperationException">No event was observed.</exception>
    internal readonly PreparedObserverRun Complete()
    {
        var last = _last ?? Enumerable.Empty<AppendedEvent>().Last();
        return _applicable
            ? new(last.Context.SequenceNumber, true, _retained.ToArray())
            : new(last.Context.SequenceNumber, false, []);
    }

    /// <summary>Releases any temporary retained references, including on failure.</summary>
    internal void Dispose() => _retained.Dispose();

    static bool SourceFilterIsDisabled(ObserverFilters? filters) =>
        filters?.EventSourceType is not { } eventSourceType || eventSourceType == EventSourceType.Unspecified;

    static bool StreamFilterIsDisabled(ObserverFilters? filters) =>
        filters?.EventStreamType is not { } eventStreamType || eventStreamType.IsAll;

    static bool HasMatchingTag(IEnumerable<Tag> tags, IEnumerable<string> filterTags)
    {
        foreach (var tag in tags)
        {
            if (filterTags.Contains(tag.Value)) return true;
        }

        return false;
    }

    void ObserveApplicability(AppendedEvent appendedEvent)
    {
        if (!_subscribed)
        {
            _subscribed = _context.EventTypeIds.Contains(appendedEvent.Context.EventType.Id);
        }

        if (!_sourceMatched)
        {
            _sourceMatched = appendedEvent.Context.EventSourceType == _context.Filters?.EventSourceType;
        }

        if (!_streamMatched)
        {
            _streamMatched = appendedEvent.Context.EventStreamType == _context.Filters?.EventStreamType;
        }

        if (!_tagsMatched)
        {
            _tagsMatched = HasMatchingTag(appendedEvent.Context.Tags, _context.Filters?.Tags ?? Array.Empty<string>());
        }

        _applicable = AllDimensionsMatch();
    }

    readonly bool AllDimensionsMatch() => _subscribed && _sourceMatched && _streamMatched && _tagsMatched;
}
