// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Defines a step in the replay job that handles events for a partition.
/// </summary>
public interface IHandleEventsForPartition : IJobStep<HandleEventsForPartitionArguments, HandleEventsForPartitionResult>
{
}
