// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Chronicle.Services.Host;

/// <summary>
/// The HTTP/2 limits of the kernel's gRPC listener.
/// </summary>
/// <remarks>
/// They live here, rather than inline where the listener is configured, so the client stream benchmark
/// (<c language="bash">--client-streams</c> in the kernel benchmarks) measures exactly what the kernel serves.
/// </remarks>
public static class KestrelHttp2Limits
{
    /// <summary>
    /// The number of concurrent streams one HTTP/2 connection may carry.
    /// </summary>
    public const int MaxStreamsPerConnection = 100;

    /// <summary>
    /// Applies the limits.
    /// </summary>
    /// <param name="limits">The <see cref="Http2Limits"/> of the listener.</param>
    public static void Apply(Http2Limits limits) => Apply(limits, MaxStreamsPerConnection);

    /// <summary>
    /// Applies the limits with the given number of concurrent streams per connection.
    /// </summary>
    /// <param name="limits">The <see cref="Http2Limits"/> of the listener.</param>
    /// <param name="maxStreamsPerConnection">The number of concurrent streams one connection may carry.</param>
    public static void Apply(Http2Limits limits, int maxStreamsPerConnection) =>
        limits.MaxStreamsPerConnection = maxStreamsPerConnection;
}
