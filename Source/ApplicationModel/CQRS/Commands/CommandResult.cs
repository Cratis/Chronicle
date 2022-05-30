// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Applications.Commands;

/// <summary>
/// Represents the result coming from executing a command.
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Gets the <see cref="CorrelationId"/> associated with the command.
    /// </summary>
    public CorrelationId CorrelationId { get; init; } = new(Guid.Empty.ToString());

    /// <summary>
    /// Gets whether or not the command executed successfully.
    /// </summary>
    public bool IsSuccess => IsAuthorized && IsValid && !HasExceptions;

    /// <summary>
    /// Gets whether or not the command was authorized to execute.
    /// </summary>
    public bool IsAuthorized { get; init; }

    /// <summary>
    /// Gets whether or not the command is valid.
    /// </summary>
    public bool IsValid => !ValidationErrors.Any();

    /// <summary>
    /// Gets whether or not there are any exceptions that occurred.
    /// </summary>
    public bool HasExceptions => !ExceptionMessages.Any();

    /// <summary>
    /// Gets any validation errors. If this collection is empty, there are errors.
    /// </summary>
    public IEnumerable<ValidationError> ValidationErrors { get; init; } = Array.Empty<ValidationError>();

    /// <summary>
    /// Gets any exception messages that might have occurred.
    /// </summary>
    public IEnumerable<string> ExceptionMessages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the stack trace if there was an exception.
    /// </summary>
    public string ExceptionStackTrace { get; init; } = string.Empty;
}
