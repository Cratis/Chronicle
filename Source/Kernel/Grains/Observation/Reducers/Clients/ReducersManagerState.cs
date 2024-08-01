// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents the state of the <see cref="ReducersManager"/>.
/// </summary>
public class ReducersManagerState
{
    /// <summary>
    /// Gets or sets the reducer definitions.
    /// </summary>
    public IEnumerable<ReducerDefinition> Reducers { get; set; } = [];
}
