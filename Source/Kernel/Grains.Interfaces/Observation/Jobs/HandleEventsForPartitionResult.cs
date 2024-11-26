// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents the result of handling events for a partition.
/// </summary>
/// <param name="LastHandledEventSequenceNumber">The sequence number of the last successfully handled event.</param>
public record HandleEventsForPartitionResult(EventSequenceNumber LastHandledEventSequenceNumber);
