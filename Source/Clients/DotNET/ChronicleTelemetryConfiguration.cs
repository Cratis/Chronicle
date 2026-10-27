// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Carries a telemetry callback for host-specific client options registrations.
/// </summary>
/// <param name="Configure">The telemetry options callback.</param>
public sealed record ChronicleTelemetryConfiguration(Action<ChronicleTelemetryOptions> Configure);
