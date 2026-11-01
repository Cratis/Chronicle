// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Refuses a write that would create a new event sequence with a name the storage provider keeps for itself.
/// </summary>
/// <remarks>
/// <para>
/// The check lives in a singleton command filter rather than in the command validators, because Arc builds a new
/// validator instance for every validation call. For a command that is not a write to a non-reserved name the filter
/// returns a <see langword="null"/> result, which Arc's command filters skip without merging, and it does not allocate.
/// </para>
/// <para>
/// Hosts discover this filter even for application commands in containers without kernel storage. Required dependencies
/// are resolved only for write commands, once and thread-safely. Missing configuration fails closed; in-memory hosts
/// explicitly register a provider that reserves no names.
/// </para>
/// </remarks>
/// <param name="serviceProvider">The host services supplying storage and its reserved-name policy for write commands.</param>
[Singleton]
internal sealed class ReservedEventSequenceNamesFilter(IServiceProvider serviceProvider) : ICommandFilter
{
    static readonly Task<CommandResult> _noVerdict = Task.FromResult<CommandResult>(null!);
    static readonly string[] _members = ["eventSequenceId"];

    readonly Lazy<(IStorage Storage, IReservedEventSequenceIds ReservedNames)> _dependencies = new(() =>
        (serviceProvider.GetRequiredService<IStorage>(), serviceProvider.GetRequiredService<IReservedEventSequenceIds>()));

    /// <inheritdoc/>
    public Task<CommandResult> OnExecution(CommandContext context)
    {
        if (context.Command is not IEventSequenceWriteCommand command)
        {
            return _noVerdict;
        }

        var (storage, reservedNames) = _dependencies.Value;
        if (!ReservedEventSequenceRules.MayBeReserved(reservedNames, command.EventStore, command.Namespace, command.EventSequenceId))
        {
            return _noVerdict;
        }

        return Refuse(storage, context, command);
    }

    static async Task<CommandResult> Refuse(IStorage storage, CommandContext context, IEventSequenceWriteCommand command)
    {
        if (await ReservedEventSequenceRules.Exists(storage, command.EventStore, command.Namespace, command.EventSequenceId))
        {
            return null!;
        }

        var result = CommandResult.Success(context.CorrelationId);
        result.ValidationResults =
        [
            new ValidationResult(
                ValidationResultSeverity.Error,
                ReservedEventSequenceRules.Message(command.EventSequenceId.Value),
                _members,
                null!)
        ];
        return result;
    }
}
