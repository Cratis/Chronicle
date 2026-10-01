// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grpc;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Observation.Alerts;

/// <summary>
/// Tracks the alert incidents for one observer from its durable transition history.
/// </summary>
[KeyedBy<ObserverKey>]
public interface IObserverAlerts : IGrainWithStringKey
{
    /// <summary>
    /// Reconciles the observer's current state with its recorded incidents.
    /// </summary>
    /// <param name="snapshot">The current observer state.</param>
    /// <returns>Awaitable task, completed at dispatch rather than after reconciliation.</returns>
    [OneWay]
    Task Reconcile(ObserverAlertSnapshot snapshot);

    /// <summary>
    /// Clears every open incident because the observer was removed or retired.
    /// </summary>
    /// <returns>Awaitable task, completed at dispatch rather than after reconciliation.</returns>
    [OneWay]
    Task Removed();
}
