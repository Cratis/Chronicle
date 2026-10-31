// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the telemetry configuration.
/// </summary>
public class Telemetry
{
    /// <summary>
    /// Gets whether gRPC client tracing is enabled. Defaults to false.
    /// </summary>
    /// <remarks>
    /// Client spans for every kernel-to-kernel or outbound gRPC call account for a large share
    /// of trace volume, so this instrumentation is opt-in.
    /// </remarks>
    public bool GrpcClientTracing { get; init; }
}
