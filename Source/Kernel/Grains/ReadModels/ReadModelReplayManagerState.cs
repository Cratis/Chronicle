// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Grains.ReadModels;

/// <summary>
/// Represents the state of the <see cref="ReadModelReplayManager"/>.
/// </summary>
public class ReadModelReplayManagerState
{
    /// <summary>
    /// Gets or sets the occurrences of replayed read models.
    /// </summary>
    public IList<ReadModelOccurrence> Occurrences { get; set; } = [];

    /// <summary>
    /// Gets or sets the new occurrences of replayed read models.
    /// </summary>
    public IList<ReadModelOccurrence> NewOccurrences { get; set; } = [];
}
