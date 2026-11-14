// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Controls identifier exposure in kernel telemetry.
/// </summary>
public class Telemetry
{
    /// <summary>
    /// Gets or inits identifier exposure in spans. Identifiers are omitted by default.
    /// </summary>
    public IdentifierExposure EventSourceIdsInSpans { get; init; } = IdentifierExposure.Omit;

    /// <summary>
    /// Gets or inits identifier exposure in logs. Identifiers are omitted by default.
    /// </summary>
    public IdentifierExposure EventSourceIdsInLogs { get; init; } = IdentifierExposure.Omit;

    /// <summary>
    /// Gets or inits the deployment key used to hash identifiers. The key is never emitted in telemetry.
    /// </summary>
    public string? IdentifierHashKey { get; init; }
}
