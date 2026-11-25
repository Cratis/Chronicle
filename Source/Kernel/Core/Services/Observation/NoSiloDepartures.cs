// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Represents an <see cref="ISiloDepartures"/> for a host without a cluster, where no silo ever leaves.
/// </summary>
internal sealed class NoSiloDepartures : ISiloDepartures
{
    /// <summary>
    /// The shared instance.
    /// </summary>
    public static readonly NoSiloDepartures Instance = new();

    /// <inheritdoc/>
    public Task Next { get; } = new TaskCompletionSource().Task;
}
