// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

/// <summary>
/// Represents a reference to event context (ctx.property).
/// </summary>
/// <param name="Property">The context property name.</param>
public record EventContextExpression(string Property) : Expression;
