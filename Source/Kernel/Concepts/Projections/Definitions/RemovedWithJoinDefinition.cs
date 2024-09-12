// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents the definition of what removes an element in a child relationship through joining of an event.
/// </summary>
/// <param name="Key">Key expression, represents the key to use for identifying the model instance.</param>
public record RemovedWithJoinDefinition(PropertyExpression Key);
