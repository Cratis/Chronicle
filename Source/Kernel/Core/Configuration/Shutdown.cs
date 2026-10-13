// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Configuration for draining local observer work at shutdown.
/// </summary>
public class Shutdown
{
    /// <summary>
    /// Gets the shared drain budget, capped at the host shutdown timeout minus two seconds.
    /// </summary>
    public TimeSpan DrainTimeout { get; init; } = TimeSpan.FromSeconds(8);
}
