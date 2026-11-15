// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Restricts the System event store to administrators for every command the kernel handles.
/// </summary>
/// <remarks>
/// This is part of the authorization stage, which the pipeline evaluates before the other filters and the validation,
/// so the refusal does not depend on the order filters are discovered in. Commands from other assemblies are ignored.
/// This filter never grants anything: it only refuses.
/// </remarks>
[Singleton]
public class SystemStoreAuthorizationCommandFilter : IAuthorizationCommandFilter
{
    static readonly Task<CommandResult> _noVerdict = Task.FromResult<CommandResult>(null!);

    /// <inheritdoc/>
    public Task<CommandResult> OnExecution(CommandContext context)
    {
        foreach (var getter in KernelCommandRouting.For(context))
        {
            if (getter.IsStore && SystemStoreAccess.IsSystem(getter.Get(context.Command))) return AuthorizeSystem(context);
        }

        return _noVerdict;
    }

    static async Task<CommandResult> AuthorizeSystem(CommandContext context)
    {
        if (context.ServiceProvider is { } services)
        {
            var access = services.GetRequiredService<ISystemStoreAccess>();
            if (await access.IsAllowed(SystemStoreRouting.CallerFor(services, context.AuthorizedPrincipal), services)) return null!;
        }

        return CommandResult.Unauthorized(context.CorrelationId, SystemStoreAccess.Refused);
    }
}
