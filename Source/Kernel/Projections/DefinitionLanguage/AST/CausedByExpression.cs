// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage.AST;

/// <summary>
/// Represents a reference to causedBy (the identity that caused the event).
/// </summary>
/// <param name="Property">The optional property name (subject, name, userName) or null for the entire Identity.</param>
public record CausedByExpression(string? Property) : Expression;
