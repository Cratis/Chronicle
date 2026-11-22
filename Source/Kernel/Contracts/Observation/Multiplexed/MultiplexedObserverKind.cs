// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Multiplexed;

/// <summary>
/// Identifies what a multiplexed stream observes with.
/// </summary>
public enum MultiplexedObserverKind
{
    /// <summary>
    /// Not specified. A stream cannot be opened with it.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The stream is a reactor, equal to a call of <c language="csharp">Reactors.Observe</c>.
    /// </summary>
    Reactor = 1,

    /// <summary>
    /// The stream is a reducer, equal to a call of <c language="csharp">Reducers.Observe</c>.
    /// </summary>
    Reducer = 2
}
