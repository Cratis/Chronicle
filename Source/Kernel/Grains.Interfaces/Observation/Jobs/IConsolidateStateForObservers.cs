// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Jobs;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Defines the job for consolidating observer state.
/// </summary>
public interface IConsolidateStateForObservers : IJob<ConsolidateStateForObserveRequest>;
