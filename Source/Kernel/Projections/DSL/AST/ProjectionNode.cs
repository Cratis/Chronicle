// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DSL.AST;

/// <summary>
/// Represents a projection definition.
/// </summary>
/// <param name="Name">The projection name.</param>
/// <param name="ReadModelType">The target read model type.</param>
/// <param name="Directives">Collection of projection-level directives and blocks.</param>
public record ProjectionNode(
    string Name,
    TypeRef ReadModelType,
    IReadOnlyList<ProjectionDirective> Directives) : AstNode;
