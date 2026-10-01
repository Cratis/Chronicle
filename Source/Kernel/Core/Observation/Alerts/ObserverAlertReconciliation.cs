// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Alerts;

/// <summary>
/// Describes the durable outcome of reconciling a versioned observer report.
/// </summary>
public enum ObserverAlertReconciliation
{
    /// <summary>
    /// Every transition currently required by the report is durable.
    /// </summary>
    Applied = 0,

    /// <summary>
    /// The source no longer authorizes this report.
    /// </summary>
    Superseded = 1,

    /// <summary>
    /// Application is incomplete or uncertain; the observer must report again.
    /// </summary>
    RetryRequired = 2
}
