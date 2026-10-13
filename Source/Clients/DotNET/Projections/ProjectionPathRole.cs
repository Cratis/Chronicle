// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Identifies whether a projection path reads event data or addresses read-model state.
/// </summary>
internal enum ProjectionPathRole
{
    /// <summary>
    /// An event or event-context value, which can call a derived function on a receiver.
    /// </summary>
    Source = 0,

    /// <summary>
    /// A read-model member path, which never calls derived functions.
    /// </summary>
    Target = 1
}
