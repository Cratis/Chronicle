// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions;

/// <summary>
/// Exception that gets thrown when an read model property expression is not supported.
/// </summary>
/// <param name="expression">The unsupported expression.</param>
public class UnsupportedReadModelPropertyExpression(string expression) : Exception($"Unknown read model property expression '{expression}'");
