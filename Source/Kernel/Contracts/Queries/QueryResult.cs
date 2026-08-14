// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Validation;

namespace Cratis.Chronicle.Contracts.Queries;

/// <summary>
/// Wire-level representation of the result coming from performing a query.
/// </summary>
/// <typeparam name="TData">Type of the data returned by the query.</typeparam>
[ProtoContract]
public class QueryResult<TData>
{
    /// <summary>
    /// Gets or sets the correlation id associated with the query.
    /// </summary>
    [ProtoMember(1)]
    public Guid CorrelationId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets whether the query was authorized to execute.
    /// </summary>
    [ProtoMember(2)]
    [DefaultValue(true)]
    public bool IsAuthorized { get; set; } = true;

    /// <summary>
    /// Gets or sets any validation results for the query.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public IList<ValidationResult> ValidationResults { get; set; } = [];

    /// <summary>
    /// Gets or sets any exception messages that might have occurred.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public IList<string> ExceptionMessages { get; set; } = [];

    /// <summary>
    /// Gets or sets the stack trace if there was an exception.
    /// </summary>
    [ProtoMember(5)]
    public string ExceptionStackTrace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data returned by the query.
    /// </summary>
    [ProtoMember(6)]
    public TData Data { get; set; } = default!;

    /// <summary>
    /// Gets whether the query executed successfully.
    /// </summary>
    public bool IsSuccess => IsAuthorized && IsValid && !HasExceptions;

    /// <summary>
    /// Gets whether the query is valid.
    /// </summary>
    public bool IsValid => ValidationResults.Count == 0;

    /// <summary>
    /// Gets whether there are any exceptions that occurred.
    /// </summary>
    public bool HasExceptions => ExceptionMessages.Count > 0;

    /// <summary>
    /// Creates a new <see cref="QueryResult{TData}"/> representing a successful query execution.
    /// </summary>
    /// <param name="correlationId">The correlation id associated with the query.</param>
    /// <param name="data">The data returned by the query.</param>
    /// <returns>A <see cref="QueryResult{TData}"/>.</returns>
    public static QueryResult<TData> Success(Guid correlationId, TData data) => new() { CorrelationId = correlationId, Data = data };

    /// <summary>
    /// Creates a new <see cref="QueryResult{TData}"/> representing an error.
    /// </summary>
    /// <param name="correlationId">The correlation id associated with the query.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A <see cref="QueryResult{TData}"/>.</returns>
    public static QueryResult<TData> Error(Guid correlationId, Exception exception) => new()
    {
        CorrelationId = correlationId,
        ExceptionMessages = [exception.Message],
        ExceptionStackTrace = exception.StackTrace ?? string.Empty
    };
}
