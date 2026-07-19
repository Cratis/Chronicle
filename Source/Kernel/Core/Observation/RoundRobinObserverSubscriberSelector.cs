// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverSubscriberSelector"/> that distributes
/// partitions round-robin across the connected client instances based on the partition key.
/// </summary>
/// <remarks>
/// The selection is a deterministic hash of the partition key, so a given partition always maps to
/// the same client instance for a given set of instances - preserving per-partition ordering while
/// spreading partitions evenly across instances. The hash is process-independent so observer grains
/// and catch-up/replay jobs on different silos select the same instance.
/// </remarks>
public class RoundRobinObserverSubscriberSelector : IObserverSubscriberSelector
{
    /// <summary>
    /// The well-known name of the strategy, usable as the observers fanOutStrategy configuration value.
    /// </summary>
    public const string StrategyName = "round-robin";

    /// <inheritdoc/>
    public ObserverSubscriberTarget Select(ObserverSubscription subscription, Key partition) =>
        subscription.Targets.Count switch
        {
            0 => new(subscription.SiloAddress, null),
            1 => subscription.Targets[0],
            _ => subscription.Targets[(int)(Hash(partition.ToString() ?? string.Empty) % (uint)subscription.Targets.Count)]
        };

    static uint Hash(string value)
    {
        // FNV-1a: deterministic across processes, unlike string.GetHashCode().
        var hash = 2166136261u;
        foreach (var character in value)
        {
            hash ^= character;
            hash *= 16777619u;
        }

        return hash;
    }
}
