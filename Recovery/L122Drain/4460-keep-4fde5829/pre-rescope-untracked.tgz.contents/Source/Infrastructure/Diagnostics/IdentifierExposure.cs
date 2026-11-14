// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Defines how identifiers are exposed in telemetry.
/// </summary>
public enum IdentifierExposure
{
    /// <summary>
    /// Do not expose identifiers.
    /// </summary>
    Omit = 0,

    /// <summary>
    /// Expose identifiers as keyed hashes.
    /// </summary>
    Hashed = 1,

    /// <summary>
    /// Expose identifiers without redaction.
    /// </summary>
    Raw = 2
}
