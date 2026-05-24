// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

/// <summary>
/// Holds information about the Chronicle client <see cref="ActivitySource"/>.
/// </summary>
public static class ClientActivity
{
    /// <summary>
    /// The <see cref="ActivitySource"/> name.
    /// </summary>
    public const string SourceName = "Cratis.Chronicle.Client";

    /// <summary>
    /// Gets the <see cref="ActivitySource"/> for the Chronicle client.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName);
}
