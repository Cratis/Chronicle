// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Orleans.CodeGeneration;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Defines the job for replaying an observer.
/// </summary>
[Version(1)]
public interface ICatchUpObserver : IJob<CatchUpObserverRequest>
{
    /// <summary>
    /// Register the partitions this job owns in its observer, so live delivery stays held back for them while the job catches them up.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task RegisterPartitionOwnership();
}
