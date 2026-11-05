// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Marks a type as a node in a property graph. The label of the node is the name of the type, and the property with
/// the key attribute holds its unique key. Which events create, update and remove nodes is declared with the model-bound
/// projection attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class NodeAttribute : Attribute;
