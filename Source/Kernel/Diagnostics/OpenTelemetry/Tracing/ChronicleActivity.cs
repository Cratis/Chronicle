// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

/// <summary>
/// Holds information about the cratis <see cref="ActivitySource"/>.
/// </summary>
public static class ChronicleActivity
{
    /// <summary>
    /// The <see cref="ActivitySource"/> name.
    /// </summary>
    public const string SourceName = "Cratis.Chronicle";
    public static readonly ActivitySource Source = new(SourceName);

    public static bool TryToActivityTraceId(this CorrelationId correlationId, out ActivityTraceId? traceId)
    {
        traceId = null;
        if (Guid.TryParse(correlationId.Value, out var guid))
        {
            traceId = ActivityTraceId.CreateFromBytes(guid.ToByteArray());
        }

        return traceId.HasValue;
    }
}

