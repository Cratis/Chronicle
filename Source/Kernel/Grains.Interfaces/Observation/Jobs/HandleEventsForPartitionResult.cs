// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents the result of handling events for a partition.
/// </summary>
/// <param name="HandledEvents">Number of events handled.</param>
public record HandleEventsForPartitionResult(EventCount HandledEvents);
