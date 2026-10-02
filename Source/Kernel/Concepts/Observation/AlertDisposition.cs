// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Describes whether an observer lifecycle still desires operational alerts.
/// </summary>
public enum AlertDisposition
{
    /// <summary>
    /// The observer may raise operational incidents.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The observer desires no processing or operational incidents, even if cleanup has not completed.
    /// </summary>
    Retired = 1
}
