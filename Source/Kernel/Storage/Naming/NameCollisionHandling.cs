// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Represents how an installation reacts to event stores and namespaces whose names overlap in storage.
/// </summary>
public enum NameCollisionHandling
{
    /// <summary>
    /// Make every non-kernel owner of an overlap unavailable and start.
    /// </summary>
    Quarantine = 0,

    /// <summary>
    /// Refuse to start when any overlap exists.
    /// </summary>
    Refuse = 1
}
