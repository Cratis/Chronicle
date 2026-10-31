// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery;

/// <summary>
/// Identifies a kind of client artifact.
/// </summary>
public enum ArtifactKind
{
    /// <summary>
    /// An event type.
    /// </summary>
    EventType = 0,

    /// <summary>
    /// An append-time constraint.
    /// </summary>
    Constraint = 1,

    /// <summary>
    /// A reactor.
    /// </summary>
    Reactor = 2,

    /// <summary>
    /// A reducer.
    /// </summary>
    Reducer = 3,

    /// <summary>
    /// A projection.
    /// </summary>
    Projection = 4,

    /// <summary>
    /// A read model.
    /// </summary>
    ReadModel = 5
}
