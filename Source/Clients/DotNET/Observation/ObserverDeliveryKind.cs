// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the kind of artifact a delivery is made to.
/// </summary>
public enum ObserverDeliveryKind
{
    /// <summary>
    /// A reactor.
    /// </summary>
    Reactor = 0,

    /// <summary>
    /// A reducer.
    /// </summary>
    Reducer = 1,

    /// <summary>
    /// A read model reactor.
    /// </summary>
    ReadModelReactor = 2,

    /// <summary>
    /// An occurrence handler.
    /// </summary>
    Occurrence = 3
}
