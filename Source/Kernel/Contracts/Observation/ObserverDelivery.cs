// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Defines how an observer receives events.
/// </summary>
public enum ObserverDelivery
{
    /// <summary>
    /// Delivers events over the existing observation stream.
    /// </summary>
    Stream = 0,

    /// <summary>
    /// Delivers events to an HTTP route.
    /// </summary>
    Http = 1
}
