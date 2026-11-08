// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Controls bulk event import sessions.
/// </summary>
public class Imports
{
    /// <summary>
    /// Gets how long an import may stay open without activity before it is abandoned and its staging discarded.
    /// </summary>
    public TimeSpan StagingRetention { get; init; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets how often the kernel looks for imports that have outlived <see cref="StagingRetention"/>.
    /// </summary>
    public TimeSpan RetentionSweepInterval { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets how many staged events the commit appends in one call to the event sequence.
    /// </summary>
    public int CommitChunkSize { get; init; } = 1000;

    /// <summary>
    /// Gets the most events one staged chunk may carry.
    /// </summary>
    public int MaxChunkEvents { get; init; } = 1000;

    /// <summary>
    /// Gets the most bytes of serialized event content one staged chunk may carry.
    /// </summary>
    public int MaxChunkBytes { get; init; } = 4 * 1024 * 1024;
}
