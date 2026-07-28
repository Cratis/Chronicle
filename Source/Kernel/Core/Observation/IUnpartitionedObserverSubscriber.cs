// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Marks an <see cref="IObserverSubscriber"/> that receives every partition of an observer through one grain
/// activation, rather than one activation per partition.
/// </summary>
/// <remarks>
/// The partition in an <see cref="Cratis.Chronicle.Concepts.Observation.ObserverSubscriberKey"/> is what gives a
/// subscriber an activation per event source, and those activations spread across the silos of a cluster. A
/// subscriber that maintains state shared by several partitions cannot be spread that way when the mechanism
/// serializing access to that state is process local — two silos would run it concurrently without seeing each
/// other. Marking such a subscriber collapses the partition in its key to
/// <see cref="Cratis.Chronicle.Concepts.Observation.ObserverSubscriberKey.AllPartitions"/>, so every partition
/// resolves to the same grain identity and Orleans' single activation guarantee keeps the whole observer on one
/// silo. The trade is deliberate: the observer no longer scales out across silos, and its throughput is bounded by
/// that single activation.
/// </remarks>
public interface IUnpartitionedObserverSubscriber;
