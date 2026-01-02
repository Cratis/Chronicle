// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DSL.AST;

/// <summary>
/// Represents a string template with interpolated expressions.
/// </summary>
/// <param name="Parts">The template parts (strings and expressions).</param>
public record TemplateExpression(IReadOnlyList<TemplatePart> Parts) : Expression;
