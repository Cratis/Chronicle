// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Defines how event source ids are included in traces.
/// </summary>
public enum EventSourceIdTracing
{
    /// <summary>
    /// Include event source ids unchanged. This is the default.
    /// </summary>
    Raw = 0,

    /// <summary>
    /// Include event source ids as keyed HMAC-SHA256 hashes.
    /// </summary>
    Hashed = 1,

    /// <summary>
    /// Do not include event source ids.
    /// </summary>
    Omitted = 2
}
