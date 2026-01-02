// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Represents a collection of parsing errors.
/// </summary>
/// <param name="errors">The collection of syntax errors.</param>
public class ParsingErrors(IEnumerable<SyntaxError> errors)
{
    readonly List<SyntaxError> _errors = [.. errors];

    /// <summary>
    /// Gets an empty <see cref="ParsingErrors"/> instance.
    /// </summary>
    public static readonly ParsingErrors Empty = new([]);

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
    /// <param name="error">The error to add.</param>
    public void Add(SyntaxError error) => _errors.Add(error);

    /// <summary>
    /// Adds multiple errors to the collection.
    /// </summary>
    /// <param name="errors">The errors to add.</param>
    public void AddRange(IEnumerable<SyntaxError> errors) => _errors.AddRange(errors);
}
