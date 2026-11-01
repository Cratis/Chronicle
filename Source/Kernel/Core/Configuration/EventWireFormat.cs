// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Defines the cluster-internal encoding of appended events sent between Orleans nodes.
/// </summary>
public enum EventWireFormat
{
    /// <summary>
    /// The released JSON encoding, compatible with older nodes during rolling upgrades.
    /// </summary>
    Json = 0,

    /// <summary>
    /// Compact version 1. Enable only after every node supports reading this encoding.
    /// </summary>
    Compact = 1
}
