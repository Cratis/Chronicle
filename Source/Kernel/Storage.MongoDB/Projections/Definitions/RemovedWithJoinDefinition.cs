// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Represents the definition of what removes an element in a child relationship through joining of an event.
/// </summary>
public class RemovedWithJoinDefinition
{
    /// <summary>
    /// Gets or sets the key expression, represents the key to use for identifying the model instance.
    /// </summary>
    public PropertyExpression Key { get; set; } = string.Empty;
}
