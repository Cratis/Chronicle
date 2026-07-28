// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// The exception that is thrown when a Screenplay projection syntax node is not supported by the projection definition mapping.
/// </summary>
/// <param name="node">The <see cref="SyntaxNode"/> that is not supported.</param>
public class UnsupportedProjectionSyntax(SyntaxNode node)
    : Exception($"Projection syntax node of type '{node.GetType().Name}' at line {node.Location.Line}, column {node.Location.Column} is not supported");
