// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_TelemetryConfiguration;

/// <summary>
/// Serializes specifications that temporarily change standard OTEL environment variables.
/// </summary>
[CollectionDefinition(nameof(telemetry_environment), DisableParallelization = true)]
public class telemetry_environment;
