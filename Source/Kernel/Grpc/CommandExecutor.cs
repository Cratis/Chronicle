// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Validation;
using FluentValidation;
using ValidationResult = Cratis.Chronicle.Contracts.Validation.ValidationResult;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Executes kernel commands by validating them with their discovered FluentValidation validators
/// before invoking the handler, capturing the outcome as a <see cref="CommandResult"/>.
/// </summary>
internal static class CommandExecutor
{
    static readonly ConcurrentDictionary<Type, IValidator[]> _validatorsByCommandType = new();

    /// <summary>
    /// Executes a command by validating it and invoking the given handler, capturing the outcome.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="handle">The handler that performs the command.</param>
    /// <typeparam name="TCommand">Type of command to execute.</typeparam>
    /// <returns>The <see cref="CommandResult"/> describing the outcome.</returns>
    internal static async Task<CommandResult> Execute<TCommand>(TCommand command, Func<TCommand, Task> handle)
        where TCommand : notnull
    {
        var correlationId = Guid.NewGuid();

        var validationResults = await Validate(command);
        if (validationResults.Count > 0)
        {
            return CommandResult.Invalid(correlationId, validationResults);
        }

        try
        {
            await handle(command);
            return CommandResult.Success(correlationId);
        }
        catch (Exception ex)
        {
            return CommandResult.Error(correlationId, ex);
        }
    }

    /// <summary>
    /// Executes a command that produces a response by validating it and invoking the given handler, capturing the outcome.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="handle">The handler that performs the command and produces the response.</param>
    /// <typeparam name="TCommand">Type of command to execute.</typeparam>
    /// <typeparam name="TResponse">Type of response the command produces.</typeparam>
    /// <returns>The <see cref="CommandResult{TResponse}"/> describing the outcome.</returns>
    internal static async Task<CommandResult<TResponse>> Execute<TCommand, TResponse>(TCommand command, Func<TCommand, Task<TResponse>> handle)
        where TCommand : notnull
    {
        var correlationId = Guid.NewGuid();

        var validationResults = await Validate(command);
        if (validationResults.Count > 0)
        {
            return CommandResult<TResponse>.Invalid(correlationId, validationResults);
        }

        try
        {
            return CommandResult<TResponse>.Success(correlationId, await handle(command));
        }
        catch (Exception ex)
        {
            return CommandResult<TResponse>.Error(correlationId, ex);
        }
    }

    static async Task<IList<ValidationResult>> Validate<TCommand>(TCommand command)
        where TCommand : notnull
    {
        var validators = _validatorsByCommandType.GetOrAdd(typeof(TCommand), DiscoverValidators);
        var results = new List<ValidationResult>();

        foreach (var validator in validators.Cast<IValidator<TCommand>>())
        {
            var result = await validator.ValidateAsync(command);
            results.AddRange(result.Errors.Select(failure => new ValidationResult
            {
                Severity = failure.Severity switch
                {
                    Severity.Warning => ValidationResultSeverity.Warning,
                    Severity.Info => ValidationResultSeverity.Information,
                    _ => ValidationResultSeverity.Error
                },
                Message = failure.ErrorMessage,
                Members = [failure.PropertyName]
            }));
        }

        return results;
    }

    static IValidator[] DiscoverValidators(Type commandType) =>
        [.. commandType.Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(IValidator<>).MakeGenericType(commandType).IsAssignableFrom(type))
            .Select(type => (IValidator)Activator.CreateInstance(type)!)];
}
