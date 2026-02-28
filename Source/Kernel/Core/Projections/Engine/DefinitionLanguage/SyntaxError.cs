// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage;

/// <summary>
/// Represents a syntax error in the projection declaration language.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Line">The line number where the error occurred.</param>
/// <param name="Column">The column number where the error occurred.</param>
public record SyntaxError(string Message, int Line, int Column)
{
    /// <summary>
    /// Returns a string representation of the syntax error.
    /// </summary>
    /// <returns>A formatted error message.</returns>
    public override string ToString() => $"{Message} at line {Line}, column {Column}";
}
