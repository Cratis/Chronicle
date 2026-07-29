// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage;

/// <summary>
/// The exception that is thrown when a Screenplay capture syntax node is not supported by the capture definition mapping.
/// </summary>
/// <param name="node">The <see cref="SyntaxNode"/> that is not supported.</param>
public class UnsupportedCaptureSyntax(SyntaxNode node)
    : Exception($"Capture syntax node of type '{node.GetType().Name}' at line {node.Location.Line}, column {node.Location.Column} is not supported");
