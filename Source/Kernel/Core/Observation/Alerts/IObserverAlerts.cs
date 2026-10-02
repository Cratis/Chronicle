// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Observation.Alerts;

/// <summary>
/// Reconciles one observer's committed current level against durable incident history.
/// </summary>
[KeyedBy<ObserverKey>]
public interface IObserverAlerts : IGrainWithStringKey
{
    /// <summary>
    /// Applies the transitions currently required by a source-authorized report.
    /// </summary>
    /// <param name="snapshot">The immutable, versioned observer report.</param>
    /// <returns>The version-qualified application receipt. RetryRequired retains work at the observer.</returns>
    Task<ObserverAlertReceipt> Reconcile(ObserverAlertSnapshot snapshot);
}
