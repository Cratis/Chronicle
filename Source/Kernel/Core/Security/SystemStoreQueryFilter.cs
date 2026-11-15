// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Restricts the System event store to administrators for every kernel query, and refuses routing values that
/// contain the grain key separator.
/// </summary>
/// <remarks>
/// This is part of the authorization stage, which the pipeline evaluates before the other filters and the validation,
/// so the refusal does not depend on the order filters are discovered in. Queries from other namespaces are ignored.
/// This filter never grants anything: it only refuses.
/// </remarks>
[Singleton]
public class SystemStoreQueryFilter : IAuthorizationQueryFilter
{
    const string KernelQueryPrefix = "Cratis.Chronicle.";

    static readonly Task<QueryResult> _noVerdict = Task.FromResult<QueryResult>(null!);

    /// <inheritdoc/>
    public Task<QueryResult> OnPerform(QueryContext context)
    {
        if (context.Arguments is not { Count: > 0 } arguments) return _noVerdict;
        if (!context.Name.Value.StartsWith(KernelQueryPrefix, StringComparison.Ordinal)) return _noVerdict;

        var targetsSystem = false;
        string? store = null;
        string? @namespace = null;
        foreach (var argument in arguments)
        {
            var name = RoutingName(argument.Key);
            if (name is null) continue;

            var value = SystemStoreRouting.AsString(argument.Value);
            if (SystemStoreAccess.HasKeySeparator(value))
            {
                return Task.FromResult(Separator(context, name));
            }

            targetsSystem |= SystemStoreRouting.IsStoreName(name) && SystemStoreAccess.IsSystem(value);
            if (string.Equals(name, "EventStore", StringComparison.Ordinal)) store = value;
            else if (string.Equals(name, "Namespace", StringComparison.Ordinal)) @namespace = value;
        }

        if (targetsSystem) return AuthorizeSystem(context);

        // A query that declares no capability reads the store it addresses.
        var capability = StoreCapabilityDecision.CapabilityOf(context.Name.Value) ?? (store is not null ? WellKnownCapabilities.ReadEventStore.Value : null);
        if (arguments.Any(argument => string.Equals(argument.Key, "ApplicationScope", StringComparison.OrdinalIgnoreCase) && argument.Value is true)) @namespace = null;
        return capability is not null ? AuthorizeStore(context, capability, store, @namespace) : _noVerdict;
    }

    static string? RoutingName(string key)
    {
        foreach (var name in SystemStoreRouting.Names)
        {
            if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase)) return name;
        }

        return null;
    }

    static async Task<QueryResult> AuthorizeSystem(QueryContext context)
    {
        if (context.ServiceProvider is { } services)
        {
            var access = services.GetRequiredService<ISystemStoreAccess>();
            if (await access.IsAllowed(SystemStoreRouting.CallerFor(services, context.AuthorizedPrincipal), services)) return null!;
        }

        return QueryResult.Unauthorized(context.CorrelationId);
    }

    static async Task<QueryResult> AuthorizeStore(QueryContext context, string capability, string? store, string? @namespace)
    {
        if (store is not null && context.ServiceProvider is { } services)
        {
            var user = SystemStoreRouting.CallerFor(services, context.AuthorizedPrincipal);
            if (await StoreCapabilityDecision.Permits(services, user, capability, store, @namespace)) return null!;
        }

        return QueryResult.Unauthorized(context.CorrelationId);
    }

    static QueryResult Separator(QueryContext context, string member)
    {
        var result = QueryResult.Success(context.CorrelationId);
        result.ValidationResults = [ValidationResult.Error(SystemStoreAccess.SeparatorRefused, [member])];
        return result;
    }
}
