// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.LanguageDefinition.AST;

/// <summary>
/// Represents a nested children block.
/// </summary>
/// <param name="CollectionName">The name of the children collection property.</param>
/// <param name="IdentifierExpression">The expression that identifies children.</param>
/// <param name="AutoMap">Whether to auto map.</param>
/// <param name="ChildBlocks">Collection of nested child blocks.</param>
public record NestedChildrenBlock(
    string CollectionName,
    Expression IdentifierExpression,
    bool AutoMap,
    IReadOnlyList<ChildBlock> ChildBlocks) : ChildBlock;
