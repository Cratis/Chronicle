// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents configuration for read models.
/// </summary>
public class ReadModels
{
    /// <summary>
    /// Gets the number of replayed read model versions to retain.
    /// Older replayed collections are removed when a new replay starts.
    /// </summary>
    public int ReplayedVersionsToKeep { get; init; } = 1;
}
