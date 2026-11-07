// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Exception that gets thrown when a property of a graph starts with the prefix Chronicle keeps for its own bookkeeping.
/// </summary>
/// <param name="name">The name of the property.</param>
public class ReservedGraphPropertyName(string name)
    : Exception($"The graph property '{name}' starts with '{GraphBookkeeping.Prefix}', which Chronicle reserves for the bookkeeping of the Neo4j graph sink. Rename the property.");
