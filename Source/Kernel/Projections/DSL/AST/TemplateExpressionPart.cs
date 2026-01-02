// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DSL.AST;

/// <summary>
/// Represents an interpolated expression in a template.
/// </summary>
/// <param name="Expression">The expression to interpolate.</param>
public record TemplateExpressionPart(Expression Expression) : TemplatePart;
