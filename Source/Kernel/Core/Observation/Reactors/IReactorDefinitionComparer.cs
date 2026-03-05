// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors;

/// <summary>
/// Defines a system that is capable of comparing <see cref="ReactorDefinition">reactor definitions</see>.
/// </summary>
public interface IReactorDefinitionComparer
{
    /// <summary>
    /// Compare two <see cref="ReactorDefinition">ReducerDefinition definitions</see>.
    /// </summary>
    /// <param name="reactorKey">The <see cref="ReactorKey"/>.</param>
    /// <param name="first">The first <see cref="ReactorDefinition"/>.</param>
    /// <param name="second">The second <see cref="ReactorDefinition"/>.</param>
    /// <returns>The <see cref="ReactorDefinitionCompareResult"/>.</returns>
    Task<ReactorDefinitionCompareResult> Compare(ReactorKey reactorKey, ReactorDefinition first, ReactorDefinition second);
}
