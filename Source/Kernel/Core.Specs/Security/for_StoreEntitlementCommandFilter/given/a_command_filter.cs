// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_StoreEntitlementCommandFilter.given;

public class a_command_filter : Specification
{
    protected internal record AppendLike(string EventStore, string Namespace);

    protected internal record ReadAndAppendLike(string EventStore, string Namespace);

    protected internal record ManageLike(string EventStore, string Namespace);

    protected internal record CopyAcrossStores(string EventStore, string Namespace, string TargetEventStore);

    protected internal record WithoutStore(string Name);

    protected IAuthorizationDecisions _decisions;
    protected HashSet<(string Store, string Capability)> _grants;
    protected CapturingLoggerFactory _loggers;
    protected StoreEntitlementCommandFilter _filter;
    protected CommandResult _result;
    protected bool _enforce;
    protected bool _inProcess;
    protected bool _hasHttpContext;
    protected bool _bindByConvention;

    protected IServiceProvider Services()
    {
        var services = new ServiceCollection();
        if (_bindByConvention)
        {
            services.AddTypeDiscovery();
            services.AddBindingsByConvention();
            services.AddSelfBindings();
        }
        services.AddSingleton(_decisions)
            .AddSingleton<ILoggerFactory>(_loggers)
            .AddSingleton(Options.Create(new ChronicleOptions { Authentication = new Authentication { EnforceStoreEntitlements = _enforce } }));
        if (_hasHttpContext)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "caller")], "spec"));
            services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } });
        }

        if (_inProcess) services.AddSingleton(new InProcessKernelHost());
        return services.BuildServiceProvider();
    }

    protected CommandContext ContextFor(object command) =>
        new(CorrelationId.New(), command.GetType(), command, [], new CommandContextValues(), ServiceProvider: Services());

    protected void Grant(string store, params StoreAccess[] accesses)
    {
        foreach (var access in accesses) _grants.Add((store, CapabilityOf(access)));
    }

    protected static string CapabilityOf(StoreAccess access)
    {
        if (access == StoreAccess.Read) return WellKnownCapabilities.ReadEventStore.Value;
        if (access == StoreAccess.Append) return WellKnownCapabilities.AppendEventStore.Value;
        return WellKnownCapabilities.ManageEventStore.Value;
    }

    protected IEnumerable<(string Store, string Capability)> Decided() => _decisions.ReceivedCalls()
        .Select(call => call.GetArguments())
        .Select(arguments => (((AuthorizationScope)arguments[2]).EventStore.Value, ((Capability)arguments[1]).Value));

    protected async Task<bool> AuditLogged(string capability)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (_loggers.Messages.Any(message => message.Contains(capability, StringComparison.Ordinal))) return true;
            await Task.Delay(50);
        }

        return false;
    }

    static StoreAccess AccessOf(Type type)
    {
        if (type == typeof(AppendLike)) return StoreAccess.Append;
        return type == typeof(ReadAndAppendLike) ? StoreAccess.Read | StoreAccess.Append : StoreAccess.Manage;
    }

    void Establish()
    {
        _grants = [];
        _hasHttpContext = true;
        _loggers = new CapturingLoggerFactory();
        _decisions = Substitute.For<IAuthorizationDecisions>();
        _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>()).Returns(call =>
        {
            var capability = call.ArgAt<Capability>(1);
            var scope = call.ArgAt<AuthorizationScope>(2);
            return _grants.Contains((scope.EventStore.Value, capability.Value))
                ? AuthorizationDecision.Allow(AuthorizationReason.Granted, capability, scope)
                : AuthorizationDecision.Deny(AuthorizationReason.NotGranted, capability, scope);
        });

        _filter = new StoreEntitlementCommandFilter(KernelCommandRouting.GettersFor, AccessOf);
    }
}
