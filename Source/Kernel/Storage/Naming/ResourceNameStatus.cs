// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// The availability of an event store or namespace according to the published <see cref="IResourceNameSnapshot"/>.
/// </summary>
public enum ResourceNameStatus
{
    /// <summary>
    /// The name can be used.
    /// </summary>
    Available = 0,

    /// <summary>
    /// The names of the installation have not been checked yet.
    /// </summary>
    NotReady = 1,

    /// <summary>
    /// The storage of the name overlaps the storage of another name, so it is unavailable.
    /// </summary>
    Quarantined = 2
}
