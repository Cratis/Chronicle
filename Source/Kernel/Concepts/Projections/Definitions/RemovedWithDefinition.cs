// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents the definition of what removes an element in a child relationship.
/// </summary>
/// <param name="Key">Key expression, represents the key to use for identifying the model instance.</param>
/// <param name="ParentKey">Optional parent key expression, typically used in child relationships for identifying parent read model.</param>
public record RemovedWithDefinition(PropertyExpression Key, PropertyExpression? ParentKey);
