// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Defines the job for retrying a failed partition for an observer.
/// </summary>
public interface IRetryFailedPartitionJob : IJob<RetryFailedPartitionRequest>
{
}
