// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Represents an implementation of <see cref="IBranch"/>.
/// </summary>
public class Branch : IBranch
{
    readonly IEventSerializer _serializer;
    readonly IEventTypes _eventTypes;
    readonly Store.Grains.IEventSequence _sequence;
    readonly Store.Grains.Branching.IBranch _actualBranch;

    /// <inheritdoc/>
    public BranchTypeId Type { get; }

    /// <inheritdoc/>
    public BranchId Identifier { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Branch"/> class.
    /// </summary>
    /// <param name="type">The <see cref="BranchTypeId"/>.</param>
    /// <param name="identifier">The unique <see cref="BranchId"/>.</param>
    /// <param name="serializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for identifying event types.</param>
    /// <param name="sequence">The target <see cref="Store.Grains.IEventSequence"/>.</param>
    /// <param name="actualBranch">The actual underlying <see cref="Store.Grains.Branching.IBranch"/>.</param>
    public Branch(
        BranchTypeId type,
        BranchId identifier,
        IEventSerializer serializer,
        IEventTypes eventTypes,
        Store.Grains.IEventSequence sequence,
        Store.Grains.Branching.IBranch actualBranch)
    {
        Type = type;
        _serializer = serializer;
        _eventTypes = eventTypes;
        _sequence = sequence;
        _actualBranch = actualBranch;
        Identifier = identifier;
    }

    /// <inheritdoc/>
    public async Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = null)
    {
        var eventType = _eventTypes.GetEventTypeFor(@event.GetType());
        var eventAsJson = await _serializer.Serialize(@event!);
        await _sequence.Append(eventSourceId, eventType, eventAsJson, validFrom);
    }

    /// <inheritdoc/>
    public async Task Merge()
    {
        await _actualBranch.Merge();
    }
}
