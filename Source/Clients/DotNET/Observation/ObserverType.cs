// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines the type of an observer.
/// </summary>
public enum ObserverType
{
    /// <summary>
    /// The type of the observer is not known.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The observer is a reactor.
    /// </summary>
    Reactor = 1,

    /// <summary>
    /// The observer is a projection.
    /// </summary>
    Projection = 2,

    /// <summary>
    /// The observer is a reducer.
    /// </summary>
    Reducer = 3,

    /// <summary>
    /// The observer is driven by something outside Chronicle.
    /// </summary>
    External = 4
}
