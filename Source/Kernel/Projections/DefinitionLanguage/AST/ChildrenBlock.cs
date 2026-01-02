// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.AST;

/// <summary>
/// Represents a children block.
/// </summary>
/// <param name="CollectionName">The name of the children collection property.</param>
/// <param name="IdentifierExpression">The expression that identifies children.</param>
/// <param name="AutoMap">Whether to auto map at children level.</param>
/// <param name="ChildBlocks">Collection of child event handlers and joins.</param>
public record ChildrenBlock(
    string CollectionName,
    Expression IdentifierExpression,
    bool AutoMap,
    IReadOnlyList<ChildBlock> ChildBlocks) : ProjectionDirective;
