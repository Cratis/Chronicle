// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications.Validation;

namespace Aksio.Cratis.Applications.Queries;

/// <summary>
/// Represents the result coming from performing a query.
/// </summary>
public class QueryResult
{
    /// <summary>
    /// The data returned.
    /// </summary>
    public object Data { get; set; } = null!;

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
    public bool IsValid => !ValidationErrors.Any();

    /// <summary>
    /// Gets whether or not there are any exceptions that occurred.
    /// </summary>
    public bool HasExceptions => ExceptionMessages.Any();

    /// <summary>
    /// Gets any validation errors. If this collection is empty, there are errors.
    /// </summary>
    public IEnumerable<ValidationError> ValidationErrors { get; init; } = Enumerable.Empty<ValidationError>();

    /// <summary>
    /// Gets any exception messages that might have occurred.
    /// </summary>
    public IEnumerable<string> ExceptionMessages { get; init; } = Enumerable.Empty<string>();

    /// <summary>
    /// Gets the stack trace if there was an exception.
    /// </summary>
    public string ExceptionStackTrace { get; init; } = string.Empty;
}
