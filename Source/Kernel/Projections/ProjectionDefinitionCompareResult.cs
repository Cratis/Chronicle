// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Definitions;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the result of comparing two <see cref="ProjectionDefinition">projection definitions</see>.
/// </summary>
public enum ProjectionDefinitionCompareResult
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
