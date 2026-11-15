// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Delegate that gets called when a page of events spanning partitions is to be observed.
/// </summary>
/// <param name="batches">The <see cref="ObserverPartitionBatch"/> of each partition in the page.</param>
/// <param name="completions">The <see cref="TaskCompletionSource{T}"/> of each partition to return its <see cref="ObserverSubscriberResult"/> to.</param>
/// <param name="deliverWithin">How long the kernel waits for the results, or null when it waits without a deadline.</param>
/// <param name="origin">The <see cref="ObservationOrigin"/> the events came from.</param>
public delegate void ReactorPageObserver(IReadOnlyList<ObserverPartitionBatch> batches, IReadOnlyDictionary<Key, TaskCompletionSource<ObserverSubscriberResult>> completions, TimeSpan? deliverWithin = null, ObservationOrigin origin = ObservationOrigin.Unknown);
