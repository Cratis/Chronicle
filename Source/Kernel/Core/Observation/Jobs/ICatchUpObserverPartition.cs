// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Jobs;
namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Defines the job for catching up partition that is behind for an observer.
/// </summary>
public interface ICatchUpObserverPartition : IJob<CatchUpObserverPartitionRequest>;
