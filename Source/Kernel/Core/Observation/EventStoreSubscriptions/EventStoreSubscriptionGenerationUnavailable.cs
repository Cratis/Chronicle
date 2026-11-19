// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// The exception thrown when a subscription cannot deliver a registered generation to its target.
/// </summary>
/// <param name="source">The source event type and generation.</param>
/// <param name="target">The target event store.</param>
/// <param name="generations">The generations registered by the target.</param>
/// <param name="cause">The reason delivery is unavailable.</param>
public class EventStoreSubscriptionGenerationUnavailable(EventType source, EventStoreName target, IEnumerable<EventTypeGeneration> generations, Exception cause)
    : Exception($"Event type '{source.Id}' generation {source.Generation} cannot be delivered to event store '{target}': target registers generation(s) {Describe(generations)}; {cause.Message}", cause)
{
    static string Describe(IEnumerable<EventTypeGeneration> generations)
    {
        var values = generations.OrderBy(_ => _.Value).Select(_ => _.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        return values.Length == 0 ? "none" : string.Join(", ", values);
    }
}
