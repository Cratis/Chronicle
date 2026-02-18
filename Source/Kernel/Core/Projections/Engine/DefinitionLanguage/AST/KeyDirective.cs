// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.AST;

/// <summary>
/// Represents a key declaration at projection level.
/// </summary>
/// <param name="Expression">The key expression.</param>
public record KeyDirective(Expression Expression) : ProjectionDirective;
