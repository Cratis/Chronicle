// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Decides whether the caller of a kernel command may exercise the access the command needs at the event store it addresses.
/// </summary>
/// <remarks>
/// The System store keeps the decision of <see cref="SystemStoreAuthorizationCommandFilter"/>. The decision is shared with
/// the query filter through <see cref="StoreCapabilityDecision"/>: it is recorded by default and refused when
/// EnforceStoreEntitlements is set. A command that arrives without a caller is trusted only in a host that calls the
/// kernel directly.
/// </remarks>
public class StoreEntitlementCommandFilter : IAuthorizationCommandFilter
{
    /// <summary>
    /// The message of every refusal, whether the event store does not exist or the caller is not entitled to it.
    /// </summary>
    public const string Refused = "The caller is not entitled to this operation.";

    static readonly Task<CommandResult> _noVerdict = Task.FromResult<CommandResult>(null!);

    readonly Func<Type, KernelCommandRouting.RoutingGetter[]> _routing;
    readonly Func<Type, StoreAccess> _access;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreEntitlementCommandFilter"/> class.
    /// </summary>
    public StoreEntitlementCommandFilter()
        : this(KernelCommandRouting.For, StoreAccessMap.ForCommand)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreEntitlementCommandFilter"/> class.
    /// </summary>
    /// <param name="routing">Gives the routing members of a command type.</param>
    /// <param name="access">Gives the access a command type needs.</param>
    internal StoreEntitlementCommandFilter(Func<Type, KernelCommandRouting.RoutingGetter[]> routing, Func<Type, StoreAccess> access)
    {
        _routing = routing;
        _access = access;
    }

    /// <inheritdoc/>
    public Task<CommandResult> OnExecution(CommandContext context)
    {
        string? store = null;
        string? @namespace = null;
        string? target = null;
        foreach (var getter in _routing(context.Type))
        {
            var value = getter.Get(context.Command);
            if (string.Equals(getter.Name, "EventStore", StringComparison.Ordinal)) store = value;
            else if (string.Equals(getter.Name, "Namespace", StringComparison.Ordinal)) @namespace = value;
            else if (string.Equals(getter.Name, "TargetEventStore", StringComparison.Ordinal)) target = value;
        }

        if (string.IsNullOrEmpty(store) && string.IsNullOrEmpty(target)) return _noVerdict;
        if (SystemStoreAccess.IsSystem(store) || SystemStoreAccess.IsSystem(target)) return _noVerdict;

        return Authorize(context, store, StoreRequestScope.IsApplication(context.Command) ? null : @namespace, target);
    }

    static bool IsTrustedInProcess(IServiceProvider services, ClaimsPrincipal? caller, HttpContext? httpContext) =>
        caller is null && httpContext is null && services.GetService<InProcessKernelHost>() is not null;

    async Task<CommandResult> Authorize(CommandContext context, string? store, string? @namespace, string? target)
    {
        if (context.ServiceProvider is not { } services) return CommandResult.Unauthorized(context.CorrelationId, Refused);

        var httpContext = (services.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor)?.HttpContext;
        var caller = context.AuthorizedPrincipal ?? httpContext?.User;
        if (IsTrustedInProcess(services, context.AuthorizedPrincipal, httpContext)) return null!;

        var permitted = string.IsNullOrEmpty(target)
            ? await StoreCapabilityDecision.PermitsAccess(services, caller, _access(context.Type), store!, @namespace)
            : (string.IsNullOrEmpty(store) || await StoreCapabilityDecision.PermitsAccess(services, caller, StoreAccess.Read, store, @namespace))
                && await StoreCapabilityDecision.PermitsAccess(services, caller, StoreAccess.Append | StoreAccess.Manage, target, null);

        return permitted ? null! : CommandResult.Unauthorized(context.CorrelationId, Refused);
    }
}
