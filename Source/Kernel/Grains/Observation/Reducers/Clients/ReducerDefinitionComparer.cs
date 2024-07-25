// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Reducers;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReducerDefinitionComparer"/>.
/// </summary>
public class ReducerDefinitionComparer : IReducerDefinitionComparer
{
    /// <inheritdoc/>
    public ReducerDefinitionCompareResult Compare(ReducerDefinition first, ReducerDefinition second)
    {
        return ReducerDefinitionCompareResult.Different;
    }
}
