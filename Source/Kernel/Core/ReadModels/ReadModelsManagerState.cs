// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents the state of the read models manager.
/// </summary>
public class ReadModelsManagerState
{
    /// <summary>
    /// Gets or sets the read model definitions.
    /// </summary>
    public IEnumerable<ReadModelDefinition> ReadModels { get; set; } = [];

    /// <summary>
    /// Gets or sets the definitions that changed since the state was last written, which are the only ones the
    /// storage provider needs to persist. It is not part of the persisted state.
    /// </summary>
    public IEnumerable<ReadModelDefinition> Modified { get; set; } = [];
}
