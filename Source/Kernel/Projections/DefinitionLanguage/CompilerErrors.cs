// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Represents a collection of compiler errors.
/// </summary>
public class CompilerErrors
{
    readonly List<CompilerError> _errors = [];

    /// <summary>
    /// Gets an empty collection of compiler errors.
    /// </summary>
    public static readonly CompilerErrors Empty = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CompilerErrors"/> class.
    /// </summary>
    /// <param name="errors">Optional initial collection of errors.</param>
    public CompilerErrors(IEnumerable<CompilerError>? errors = null)
    {
        if (errors is not null)
        {
            _errors.AddRange(errors);
        }
    }

    /// <summary>
    /// Gets all the errors.
    /// </summary>
    public IReadOnlyList<CompilerError> Errors => _errors;

    /// <summary>
    /// Gets a value indicating whether there are any errors.
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Creates a CompilerErrors instance from ParsingErrors.
    /// </summary>
    /// <param name="parsingErrors">The parsing errors to convert.</param>
    /// <returns>A new CompilerErrors instance.</returns>
    public static CompilerErrors FromParsingErrors(ParsingErrors parsingErrors)
    {
        var errors = parsingErrors.Errors.Select(e => new CompilerError(e.Message, e.Line, e.Column));
        return new CompilerErrors(errors);
    }

    /// <summary>
    /// Adds an error to the collection.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The line number.</param>
    /// <param name="column">The column number.</param>
    public void Add(string message, int line, int column)
    {
        _errors.Add(new CompilerError(message, line, column));
    }

    /// <summary>
    /// Adds an error to the collection.
    /// </summary>
    /// <param name="error">The error to add.</param>
    public void Add(CompilerError error)
    {
        _errors.Add(error);
    }

    /// <summary>
    /// Adds multiple errors to the collection.
    /// </summary>
    /// <param name="errors">The errors to add.</param>
    public void AddRange(IEnumerable<CompilerError> errors)
    {
        _errors.AddRange(errors);
    }
}
