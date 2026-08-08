// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Validation;

namespace Cratis.Chronicle.Contracts.Commands;

/// <summary>
/// Wire-level representation of the result coming from executing a command that produces a response.
/// </summary>
/// <typeparam name="TResponse">Type of the response produced by the command.</typeparam>
/// <remarks>
/// This deliberately does not derive from <see cref="CommandResult"/>. protobuf-net models inheritance
/// through declared includes on the base contract, which an open generic cannot supply, so the members
/// are repeated here to keep the wire shape unambiguous.
/// </remarks>
[ProtoContract]
public class CommandResult<TResponse>
{
    /// <summary>
    /// Gets or sets the correlation id associated with the command.
    /// </summary>
    [ProtoMember(1)]
    public Guid CorrelationId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets whether the command was authorized to execute.
    /// </summary>
    [ProtoMember(2)]
    [DefaultValue(true)]
    public bool IsAuthorized { get; set; } = true;

    /// <summary>
    /// Gets or sets any validation results for the command.
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
    /// Gets or sets the reason for authorization failure, if any.
    /// </summary>
    [ProtoMember(6)]
    public string AuthorizationFailureReason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the response produced by the command.
    /// </summary>
    [ProtoMember(7)]
    public TResponse Response { get; set; } = default!;

    /// <summary>
    /// Gets whether the command executed successfully.
    /// </summary>
    public bool IsSuccess => IsAuthorized && IsValid && !HasExceptions;

    /// <summary>
    /// Gets whether the command is valid.
    /// </summary>
    public bool IsValid => ValidationResults.Count == 0;

    /// <summary>
    /// Gets whether there are any exceptions that occurred.
    /// </summary>
    public bool HasExceptions => ExceptionMessages.Count > 0;

    /// <summary>
    /// Creates a new <see cref="CommandResult{TResponse}"/> representing a successful command execution.
    /// </summary>
    /// <param name="correlationId">The correlation id associated with the command.</param>
    /// <param name="response">The response produced by the command.</param>
    /// <returns>A <see cref="CommandResult{TResponse}"/>.</returns>
    public static CommandResult<TResponse> Success(Guid correlationId, TResponse response) =>
        new() { CorrelationId = correlationId, Response = response };

    /// <summary>
    /// Creates a new <see cref="CommandResult{TResponse}"/> representing an error.
    /// </summary>
    /// <param name="correlationId">The correlation id associated with the command.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A <see cref="CommandResult{TResponse}"/>.</returns>
    public static CommandResult<TResponse> Error(Guid correlationId, Exception exception)
    {
        var result = CommandResult.Error(correlationId, exception);
        return new()
        {
            CorrelationId = result.CorrelationId,
            ExceptionMessages = result.ExceptionMessages,
            ExceptionStackTrace = result.ExceptionStackTrace
        };
    }

    /// <summary>
    /// Creates a new <see cref="CommandResult{TResponse}"/> representing a failed validation.
    /// </summary>
    /// <param name="correlationId">The correlation id associated with the command.</param>
    /// <param name="validationResults">The validation results describing the failures.</param>
    /// <returns>A <see cref="CommandResult{TResponse}"/>.</returns>
    public static CommandResult<TResponse> Invalid(Guid correlationId, IEnumerable<ValidationResult> validationResults) =>
        new() { CorrelationId = correlationId, ValidationResults = [.. validationResults] };
}
