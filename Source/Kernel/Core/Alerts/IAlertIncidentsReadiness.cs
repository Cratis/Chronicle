// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Samples the health of incident materialization.
/// </summary>
public interface IAlertIncidentsReadiness
{
    /// <summary>
    /// Samples the transition tail, observer state, and failures in that order.
    /// </summary>
    /// <returns>The sampled health. Storage or grain errors propagate.</returns>
    Task<AlertIncidentsReadinessState> Get();
}
