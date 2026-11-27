// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents what is known about whether an event the observer observes is waiting.
/// </summary>
public enum ObserverLagRelevanceKind
{
    /// <summary>
    /// It could not be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// It was proven that none is waiting.
    /// </summary>
    None = 1,

    /// <summary>
    /// One is waiting.
    /// </summary>
    Pending = 2
}
