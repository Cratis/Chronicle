// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Observation;

/// <summary>
/// Defines a system for working with observers.
/// </summary>
public interface IObservers
{
    /// <summary>
    /// Get all the handlers registered.
    /// </summary>
    IEnumerable<ObserverHandler> Handlers { get; }
}
