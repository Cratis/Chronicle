// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Jobs;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Defines the job for replaying a partition for an observer.
/// </summary>
public interface IReplayObserverPartition : IJob<ReplayObserverPartitionRequest>;