// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Refuses routing values that contain the grain key separator for every command the kernel handles.
/// </summary>
/// <remarks>
/// Commands from other assemblies are ignored. The restriction of the System event store to administrators is in
/// <see cref="SystemStoreAuthorizationCommandFilter"/>. This filter never grants anything: it only refuses.
/// </remarks>
[Singleton]
public class SystemStoreCommandFilter : ICommandFilter
{
    static readonly Task<CommandResult> _noVerdict = Task.FromResult<CommandResult>(null!);

    /// <inheritdoc/>
    public Task<CommandResult> OnExecution(CommandContext context)
    {
        foreach (var getter in KernelCommandRouting.For(context))
        {
            if (SystemStoreAccess.HasKeySeparator(getter.Get(context.Command)))
            {
                return Task.FromResult(Separator(context, getter.Name));
            }
        }

        return _noVerdict;
    }

    static CommandResult Separator(CommandContext context, string member)
    {
        var result = CommandResult.Success(context.CorrelationId);
        result.ValidationResults = [ValidationResult.Error(SystemStoreAccess.SeparatorRefused, [member])];
        return result;
    }
}
