// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.LanguageDefinition.AST;

/// <summary>
/// Represents a reference to event data (e.property).
/// </summary>
/// <param name="Path">The property path within the event.</param>
public record EventDataExpression(string Path) : Expression;
