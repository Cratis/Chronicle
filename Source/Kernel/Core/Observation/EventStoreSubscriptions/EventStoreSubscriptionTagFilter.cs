// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Matches the named tags of an event against the tag filter of an event store subscription.
/// </summary>
public static class EventStoreSubscriptionTagFilter
{
    /// <summary>
    /// Determines whether an event with the given tags passes the filter.
    /// </summary>
    /// <param name="filter">The named-tag filter of the subscription.</param>
    /// <param name="eventTags">The named tags of the event.</param>
    /// <returns>True if the event should be delivered; false otherwise.</returns>
    /// <remarks>
    /// An empty filter matches everything. Otherwise every distinct tag name of the filter must be present on the
    /// event with one of the values the filter lists for that name (values of one name are alternatives, names are combined).
    /// Untagged events never match a non-empty filter.
    /// </remarks>
    public static bool Matches(IEnumerable<NamedTag> filter, IEnumerable<NamedTag> eventTags)
    {
        var groups = filter.GroupBy(_ => _.Name).ToArray();
        if (groups.Length == 0)
        {
            return true;
        }

        var tags = eventTags.ToArray();
        return groups.All(group => tags.Any(tag => tag.Name == group.Key && group.Any(wanted => wanted.Value == tag.Value)));
    }
}
