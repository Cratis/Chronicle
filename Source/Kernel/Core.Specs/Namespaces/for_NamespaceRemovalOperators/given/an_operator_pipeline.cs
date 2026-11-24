// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalOperators.given;

public class an_operator_pipeline : Specification, IDisposable
{
    protected const long Revision = 42;

    protected ServiceProvider _provider;
    protected ICommandPipeline _pipeline;
    protected INamespaceIncarnations _incarnations;
    protected INamespaceCasing _casing;
    protected IAuthorizationDecisions _decisions;
    protected HashSet<(string Store, Capability Capability)> _grants;
    protected List<(Capability Capability, AuthorizationScope Scope)> _asked;
    protected Guid _operation;
    protected Contracts.Commands.CommandResult _result;

    void Establish()
    {
        _operation = Guid.NewGuid();
        _grants = [];
        _asked = [];
        _incarnations = Substitute.For<INamespaceIncarnations>();
        _casing = Substitute.For<INamespaceCasing>();
        _casing.Canonicalize(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(call => call.ArgAt<EventStoreNamespaceName>(1));
        _decisions = Substitute.For<IAuthorizationDecisions>();
        _decisions.Decide(Arg.Any<ClaimsPrincipal?>(), Arg.Any<Capability>(), Arg.Any<AuthorizationScope>()).Returns(call =>
        {
            var capability = call.ArgAt<Capability>(1);
            var scope = call.ArgAt<AuthorizationScope>(2);
            _asked.Add((capability, scope));
            return _grants.Contains((scope.EventStore.Value, capability))
                ? AuthorizationDecision.Allow(AuthorizationReason.Granted, capability, scope)
                : AuthorizationDecision.Deny(AuthorizationReason.NotGranted, capability, scope);
        });
        var accessor = new HttpContextAccessor();
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.Configure<ArcOptions>(_ => { });
        services.AddCratisArcCore();
        services.AddSingleton(Options.Create(new ChronicleOptions { Authentication = new Authentication { EnforceStoreEntitlements = true } }));
        services.AddKernelSideCommandExtensionPointsForKernelCommands();
        services.AddSingleton(_incarnations);
        services.AddSingleton(_casing);
        services.AddSingleton(_decisions);
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore("shop").Returns(true);
        services.AddSingleton(storage);
        services.AddSingleton<IHttpContextAccessor>(accessor);
        _provider = services.BuildServiceProvider();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "operator")], "spec")),
            RequestServices = _provider
        };
        _pipeline = _provider.GetRequiredService<ICommandPipeline>();
    }

    public void Dispose() => _provider.Dispose();
}
