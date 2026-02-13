// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Grains.Observation.Reactors;

/// <summary>
/// Represents the result of comparing two <see cref="ReactorDefinition">reactor definitions</see>.
/// </summary>
public enum ReactorDefinitionCompareResult
{
    /// <summary>
    /// The definitions are the same.
    /// </summary>
    Same = 0,

    /// <summary>
    /// The definitions is new.
    /// </summary>
    New = 1,

    /// <summary>
    /// The definitions are different.
    /// </summary>
    Different = 2
}
