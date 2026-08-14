// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.SequenceQueries;

/// <summary>
/// Defines who a saved event sequence query is visible to.
/// </summary>
public enum SequenceQueryScope
{
    /// <summary>
    /// Visible only to the identity that saved it.
    /// </summary>
    User = 0,

    /// <summary>
    /// Visible to everyone using the event store.
    /// </summary>
    Everyone = 1
}
