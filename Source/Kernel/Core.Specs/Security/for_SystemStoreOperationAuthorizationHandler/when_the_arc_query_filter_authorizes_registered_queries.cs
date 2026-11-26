// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.Filters;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_SystemStoreOperationAuthorizationHandler;

public class when_the_arc_query_filter_authorizes_registered_queries : given.a_query_operation_policy
{
    ServiceProvider _provider;
    QueryResult _ordinary;
    QueryResult _destructive;
    object? _performed;

    void Establish()
    {
        var services = new ServiceCollection().AddLogging().AddOptions();
        services.Configure<ArcOptions>(_ => { });
        services.AddCratisArcCore();
        services.AddSingleton<IAuthorizationPolicyRuntime, AspNetAuthorizationPolicyRuntime>();
        services.AddSingleton(Options.Create(new ChronicleOptions
        {
            Authentication = new Authentication { Enabled = true, EnforceAdministratorPolicyForOperations = false, EnforceDestructiveOperations = true }
        }));
        services.AddSingleton(_authorization);
        services.AddSingleton(_performers);
        var current = Substitute.For<ICurrentPrincipalAccessor>();
        current.Current.Returns(_principal);
        services.AddSingleton(current);
        services.AddChronicleAdministratorAuthorization();
        _provider = services.BuildServiceProvider();
        _services = _provider;
    }

    async Task Because()
    {
        var filter = new AuthorizationFilter(_performers);
        Query(nameof(given.operation_queries.Ordinary), new() { ["eventStore"] = "application" });
        _ordinary = await filter.OnPerform(_query);
        _performed = await _performer.Perform(_query);
        Query(nameof(given.operation_queries.Destructive), new() { ["eventStore"] = "System" });
        _destructive = await filter.OnPerform(_query);
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_allow_an_ordinary_store_through_the_real_arc_hook() => _ordinary.IsAuthorized.ShouldBeTrue();
    [Fact] void should_preserve_the_effective_execution_argument() => _performed.ShouldEqual("application");
    [Fact] void should_refuse_a_destructive_system_query_through_the_real_arc_hook() => _destructive.IsAuthorized.ShouldBeFalse();
}
