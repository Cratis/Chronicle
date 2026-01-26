// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Represents a collection of parsing errors.
/// </summary>
/// <param name="errors">The collection of syntax errors.</param>
public class ParsingErrors(IEnumerable<SyntaxError> errors)
{
    /// <summary>
    /// Gets an empty <see cref="ParsingErrors"/> instance.
    /// </summary>
    public static readonly ParsingErrors Empty = new([]);

    readonly List<SyntaxError> _errors = [.. errors];

    /// <summary>
    /// Gets a value indicating whether there are any errors.
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Gets all the errors.
    /// </summary>
    public IEnumerable<SyntaxError> Errors => _errors;

    /// <summary>
    /// Adds an error to the collection.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The line number where the error occurred.</param>
    /// <param name="column">The column number where the error occurred.</param>
    public void Add(string message, int line, int column) => _errors.Add(new SyntaxError(message, line, column));
}
