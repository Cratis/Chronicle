// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Holds the process-level snapshot behind the open incident gauges.
/// </summary>
public interface IAlertIncidentsGauge
{
    /// <summary>
    /// Registers an owner as the publisher in this process.
    /// </summary>
    /// <param name="owner">The owner.</param>
    void Activate(object owner);

    /// <summary>
    /// Publishes a snapshot on behalf of the owner.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="snapshot">The snapshot.</param>
    void Publish(object owner, AlertIncidentsGaugeSnapshot snapshot);

    /// <summary>
    /// Clears the owner and its snapshot, unless a newer owner took over.
    /// </summary>
    /// <param name="owner">The owner.</param>
    void Clear(object owner);
}
