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
    /// Initializes a new instance of the <see cref="ChronicleTelemetryConfiguration"/> class that leaves the telemetry defaults unchanged.
    /// </summary>
    /// <remarks>
    /// A container that binds this type without an <see cref="Action{T}"/> of <see cref="ChronicleTelemetryOptions"/> registered uses this constructor,
    /// so a host that never calls WithTelemetry starts with the telemetry defaults.
    /// </remarks>
    public ChronicleTelemetryConfiguration()
        : this(_ => { })
    {
    }
}
