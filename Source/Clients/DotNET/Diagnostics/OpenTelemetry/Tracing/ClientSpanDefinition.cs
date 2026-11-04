// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

/// <summary>
/// The precomputed names, keys and kind of one client operation.
/// </summary>
/// <param name="LegacyName">The existing operation name.</param>
/// <param name="ConventionName">The shared convention name.</param>
/// <param name="Kind">The span kind.</param>
internal sealed record ClientSpanDefinition(string LegacyName, string ConventionName, ActivityKind Kind)
{
    /// <summary>
    /// Gets the operation's ordered legacy attribute keys.
    /// </summary>
    internal required string[] LegacyKeys { get; init; }

    /// <summary>
    /// Gets the operation's ordered convention attribute keys.
    /// </summary>
    internal required string[] ConventionKeys { get; init; }

    /// <summary>
    /// Gets the observer type for handling operations, if any.
    /// </summary>
    internal string? ObserverType { get; init; }
}
