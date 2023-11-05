// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Jobs;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Defines the job for consolidating observer state.
/// </summary>
public interface IConsolidateStateForObservers : IJob<ConsolidateStateForObserveRequest>
{
}
