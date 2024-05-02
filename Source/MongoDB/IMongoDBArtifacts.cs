// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.MongoDB;

/// <summary>
/// Defines the different artifacts that can be registered for MongoDB.
/// </summary>
public interface IMongoDBArtifacts
{
    /// <summary>
    /// Gets the class maps to register.
    /// </summary>
    IEnumerable<Type> ClassMaps { get; }

    /// <summary>
    /// Gets the convention pack filters to register.
    /// </summary>
    IEnumerable<Type> ConventionPackFilters { get; }
}
