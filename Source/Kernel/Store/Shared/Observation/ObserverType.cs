// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation;

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
    /// Client type of observer.
    /// </summary>
    Client = 1,

    /// <summary>
    /// Projection type of observer.
    /// </summary>
    Projection = 2
}
