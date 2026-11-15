// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Represents the outcome of reducing a page of partitions, settled per partition.
/// </summary>
/// <param name="Results">The <see cref="ReducerSubscriberResult"/> the client returned for each partition it answered for.</param>
/// <param name="Notifications">The committed change of each partition whose state changed and was written, to notify watchers of.</param>
/// <param name="FailedPartitions">
/// The partitions whose writes the sink reported as failed. A shared bulk window can report partitions outside the page as well.
/// </param>
public record ReducerPageOutcome(
    IReadOnlyDictionary<Key, ReducerSubscriberResult> Results,
    IReadOnlyDictionary<Key, ReadModelChangeNotification> Notifications,
    IReadOnlyCollection<FailedPartition> FailedPartitions);
