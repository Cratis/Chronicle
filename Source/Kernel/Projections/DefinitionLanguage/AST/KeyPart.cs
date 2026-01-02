// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DSL.AST;

/// <summary>
/// Represents a part of a composite key.
/// </summary>
/// <param name="PropertyName">The property name in the composite key.</param>
/// <param name="Expression">The value expression.</param>
public record KeyPart(string PropertyName, Expression Expression) : AstNode;
