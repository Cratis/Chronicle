// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Describes why a manual scope repair was rejected.
/// </summary>
public enum ReopenStreamScopeError
{
    /// <summary>
    /// The scope was reopened successfully.
    /// </summary>
    None = 0,

    /// <summary>
    /// No exact closure exists for the requested scope.
    /// </summary>
    NotCompleted = 1,

    /// <summary>
    /// The exact closure belongs to a closing event and must be reopened by an event.
    /// </summary>
    ClosedByEvent = 2
}
