// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Reducers;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents the result of comparing two <see cref="ReducerDefinition">reducer definitions</see>.
/// </summary>
public enum ReducerDefinitionCompareResult
{
    /// <summary>
    /// The definitions are the same.
    /// </summary>
    Same = 0,

    /// <summary>
    /// The definitions are different.
    /// </summary>
    Different = 1
}
