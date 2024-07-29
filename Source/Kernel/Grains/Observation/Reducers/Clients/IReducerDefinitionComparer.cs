// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Reducers;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Defines a system that is capable of comparing <see cref="ReducerDefinition">reducer definitions</see>.
/// </summary>
public interface IReducerDefinitionComparer
{
    /// <summary>
    /// Compare two <see cref="ReducerDefinition">ReducerDefinition definitions</see>.
    /// </summary>
    /// <param name="first">The first <see cref="ReducerDefinition"/>.</param>
    /// <param name="second">The second <see cref="ReducerDefinition"/>.</param>
    /// <returns>The <see cref="ReducerDefinitionCompareResult"/>.</returns>
    ReducerDefinitionCompareResult Compare(ReducerDefinition first, ReducerDefinition second);
}
