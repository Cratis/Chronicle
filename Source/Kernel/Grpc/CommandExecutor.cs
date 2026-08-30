// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using ValidationResult = Cratis.Chronicle.Contracts.Validation.ValidationResult;
using ValidationResultSeverity = Cratis.Chronicle.Contracts.Validation.ValidationResultSeverity;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Executes kernel commands through the Arc <see cref="ICommandPipeline"/> - the same authorization,
/// validation, and handler pipeline the HTTP surface runs - and maps the outcome onto the wire-level
/// <see cref="Contracts.Commands.CommandResult"/>.
/// </summary>
/// <remarks>
/// The pipeline owns validator discovery and resolution, so a <c>CommandValidator&lt;T&gt;</c> with
/// constructor dependencies behaves identically on both transports. Each execution uses the pipeline's
/// scope-free form, which creates and disposes a dedicated service scope per invocation - the handler's
/// parameters resolve from that scope exactly as they do for an HTTP request.
/// </remarks>
internal static class CommandExecutor
{
    /// <summary>
    /// Executes a command through the pipeline, capturing the outcome.
    /// </summary>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/> to execute through.</param>
    /// <param name="command">The command to execute.</param>
    /// <returns>The <see cref="Contracts.Commands.CommandResult"/> describing the outcome.</returns>
    internal static async Task<Contracts.Commands.CommandResult> Execute(ICommandPipeline pipeline, object command)
    {
        var result = await pipeline.Execute(command);
        return new Contracts.Commands.CommandResult
        {
            CorrelationId = result.CorrelationId,
            IsAuthorized = result.IsAuthorized,
            AuthorizationFailureReason = result.AuthorizationFailureReason ?? string.Empty,
            ValidationResults = [.. result.ValidationResults.Select(ToContract)],
            ExceptionMessages = [.. result.ExceptionMessages],
            ExceptionStackTrace = result.ExceptionStackTrace ?? string.Empty
        };
    }

    /// <summary>
    /// Executes a command that produces a response through the pipeline, capturing the outcome.
    /// </summary>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/> to execute through.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="mapResponse">Maps the domain response the handler produced onto its contract shape.</param>
    /// <typeparam name="TDomainResponse">Type of response the command's handler produces.</typeparam>
    /// <typeparam name="TResponse">Type of contract response the command produces.</typeparam>
    /// <returns>The <see cref="Contracts.Commands.CommandResult{TResponse}"/> describing the outcome.</returns>
    internal static async Task<Contracts.Commands.CommandResult<TResponse>> Execute<TDomainResponse, TResponse>(
        ICommandPipeline pipeline,
        object command,
        Func<TDomainResponse, TResponse> mapResponse)
    {
        var result = await pipeline.Execute<TDomainResponse>(command);
        var contractResult = new Contracts.Commands.CommandResult<TResponse>
        {
            CorrelationId = result.CorrelationId,
            IsAuthorized = result.IsAuthorized,
            AuthorizationFailureReason = result.AuthorizationFailureReason ?? string.Empty,
            ValidationResults = [.. result.ValidationResults.Select(ToContract)],
            ExceptionMessages = [.. result.ExceptionMessages],
            ExceptionStackTrace = result.ExceptionStackTrace ?? string.Empty
        };

        if (result.Response is not null)
        {
            contractResult.Response = mapResponse(result.Response);
        }

        return contractResult;
    }

    static ValidationResult ToContract(Arc.Validation.ValidationResult result) => new()
    {
        Severity = result.Severity switch
        {
            Arc.Validation.ValidationResultSeverity.Information => ValidationResultSeverity.Information,
            Arc.Validation.ValidationResultSeverity.Warning => ValidationResultSeverity.Warning,
            Arc.Validation.ValidationResultSeverity.Error => ValidationResultSeverity.Error,
            _ => ValidationResultSeverity.Unknown
        },
        Message = result.Message,
        Members = [.. result.Members]
    };
}
