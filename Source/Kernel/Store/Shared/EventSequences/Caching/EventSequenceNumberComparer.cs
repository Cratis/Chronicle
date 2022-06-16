// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching;

#pragma warning disable RCS1241

/// <summary>
/// Represents a <see cref="IComparer{T}"/> for <see cref="EventSequenceNumber"/>.
/// </summary>
public class EventSequenceNumberComparer : IComparer<EventSequenceNumber>
{
    /// <inheritdoc/>
    public int Compare(EventSequenceNumber? x, EventSequenceNumber? y) => x!.Value.CompareTo(y!.Value);
}
