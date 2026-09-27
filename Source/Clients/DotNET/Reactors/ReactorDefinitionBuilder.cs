// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Builds a runtime reactor subscription.
/// </summary>
public class ReactorDefinitionBuilder : IReactorDefinitionBuilder
{
    readonly HashSet<EventType> _eventTypes = [];
    EventSequenceId _eventSequenceId = EventSequenceId.Log;
    bool _isReplayable = true;

    /// <inheritdoc/>
    public IReactorDefinitionBuilder WithEventType(EventType eventType)
    {
        _eventTypes.Add(eventType);
        return this;
    }

    /// <inheritdoc/>
    public IReactorDefinitionBuilder OnEventSequence(EventSequenceId eventSequenceId)
    {
        _eventSequenceId = eventSequenceId;
        return this;
    }

    /// <inheritdoc/>
    public IReactorDefinitionBuilder NotReplayable()
    {
        _isReplayable = false;
        return this;
    }

    /// <summary>
    /// Builds a snapshot of the configured subscription.
    /// </summary>
    /// <param name="id">The reactor identifier for validation.</param>
    /// <returns>The configured sequence, event types, and replay policy.</returns>
    /// <exception cref="NoEventTypesForReactor">Thrown if no event types were configured.</exception>
    internal (EventSequenceId EventSequenceId, EventType[] EventTypes, bool IsReplayable) Build(ReactorId id)
    {
        if (_eventTypes.Count == 0)
        {
            throw new NoEventTypesForReactor(id);
        }

        return (_eventSequenceId, [.. _eventTypes], _isReplayable);
    }
}
