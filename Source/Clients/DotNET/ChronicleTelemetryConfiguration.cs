// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Carries a telemetry callback for host-specific client options registrations.
/// </summary>
/// <param name="Configure">The telemetry options callback.</param>
[IgnoreConvention]
public sealed record ChronicleTelemetryConfiguration(Action<ChronicleTelemetryOptions> Configure)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleTelemetryConfiguration"/> class that applies no changes.
    /// </summary>
    /// <remarks>
    /// Gives the container a constructor it can always satisfy, so a host whose type discovery still registers this
    /// carrier by convention starts instead of failing to resolve the callback.
    /// </remarks>
    public ChronicleTelemetryConfiguration()
        : this(static _ => { })
    {
    }
}
