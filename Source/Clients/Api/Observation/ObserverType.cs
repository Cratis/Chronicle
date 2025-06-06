// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Defines the different types of observers.
/// </summary>
public enum ObserverType
{
    /// <summary>
    /// Unknown type of observer.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Reactor type of observer.
    /// </summary>
    Reactor = 1,

    /// <summary>
    /// Projection type of observer.
    /// </summary>
    Projection = 2,

    /// <summary>
    /// Reducer type of observer.
    /// </summary>
    Reducer = 3
}
