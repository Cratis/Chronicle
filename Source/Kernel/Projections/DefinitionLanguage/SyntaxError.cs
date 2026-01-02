// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DSL;

/// <summary>
/// Exception that gets thrown when there is a syntax error in the projection DSL.
/// </summary>
/// <param name="message">The error message.</param>
/// <param name="line">The line number where the error occurred.</param>
/// <param name="column">The column number where the error occurred.</param>
public class SyntaxError(string message, int line, int column) : Exception($"{message} at line {line}, column {column}")
{
    /// <summary>
    /// Gets the line number where the error occurred.
    /// </summary>
    public int Line { get; } = line;

    /// <summary>
    /// Gets the column number where the error occurred.
    /// </summary>
    public int Column { get; } = column;
}
