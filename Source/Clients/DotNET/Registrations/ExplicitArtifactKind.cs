// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The explicitly nominated artifact kinds, without convention discovery.
/// </summary>
internal enum ExplicitArtifactKind
{
    /// <summary>
    /// An instance-method side-effect observer.
    /// </summary>
    Reactor = 0,

    /// <summary>
    /// An instance-method read-model reducer.
    /// </summary>
    Reducer = 1,

    /// <summary>
    /// A constraint definer.
    /// </summary>
    Constraint = 2,

    /// <summary>
    /// An event source definition.
    /// </summary>
    EventSource = 3,

    /// <summary>
    /// An event generation migration.
    /// </summary>
    Migration = 4
}
