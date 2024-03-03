// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Queries;
using Aksio.Validation;

namespace Cratis.Connections;

/// <summary>
/// Represents a query result with the data result typed.
/// </summary>
/// <typeparam name="TDataType">The type of the resulting data.</typeparam>
public class TypedQueryResult<TDataType>
{
    /// <summary>
    /// Represents a successful command result.
    /// </summary>
    public static readonly QueryResult Success = new();

    /// <summary>
    /// The data returned.
    /// </summary>
    public TDataType Data { get; set; } = default!;

    /// <summary>
    /// Gets whether or not the query executed successfully.
    /// </summary>
    public bool IsSuccess => IsAuthorized && IsValid && !HasExceptions;

    /// <summary>
    /// Gets whether or not the query was authorized to execute.
    /// </summary>
    public bool IsAuthorized { get; init; } = true;

    /// <summary>
    /// Gets whether or not the query is valid.
    /// </summary>
    public bool IsValid => !ValidationResults.Any();

    /// <summary>
    /// Gets whether or not there are any exceptions that occurred.
    /// </summary>
    public bool HasExceptions => ExceptionMessages.Any();

    /// <summary>
    /// Gets any validation errors. If this collection is empty, there are errors.
    /// </summary>
    public IEnumerable<ValidationResult> ValidationResults { get; init; } = Enumerable.Empty<ValidationResult>();

    /// <summary>
    /// Gets any exception messages that might have occurred.
    /// </summary>
    public IEnumerable<string> ExceptionMessages { get; init; } = Enumerable.Empty<string>();

    /// <summary>
    /// Gets the stack trace if there was an exception.
    /// </summary>
    public string ExceptionStackTrace { get; init; } = string.Empty;
}
