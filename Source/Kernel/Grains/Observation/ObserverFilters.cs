// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Holds filters that can be used by observers as predicates when subscribing to streams.
/// </summary>
public static class ObserverFilters
{
#pragma warning disable IDE0060 // allow unused parameter
    /// <summary>
    /// Represents a filter for event types.
    /// </summary>
    /// <param name="streamIdentity">The stream identity.</param>
    /// <param name="filterData">Data associated with the filter.</param>
    /// <param name="item">Item to filter.</param>
    /// <returns>True if its to be included, false if not.</returns>
    public static bool EventTypesFilter(IStreamIdentity streamIdentity, object filterData, object item)
    {
        var appendedEvent = (item as AppendedEvent)!;
        var eventTypes = (filterData as EventType[])!;
        if (eventTypes.Length == 0)
        {
            return true;
        }
        return eventTypes.Any(_ => _.Id.Equals(appendedEvent.Metadata.Type.Id));
    }
#pragma warning restore IDE0060
}
