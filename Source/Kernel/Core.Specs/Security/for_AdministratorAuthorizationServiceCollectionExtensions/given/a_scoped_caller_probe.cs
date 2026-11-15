// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions.given;

[IgnoreConvention]
public class a_scoped_caller_probe
{
    readonly List<ScopedCheck> _created = [];

    public enum DiscoveryOrder
    {
        None = 0,
        BeforeExtension = 1,
        AfterExtension = 2
    }

    protected async Task VerifyCallerIsolation(bool registerBefore, DiscoveryOrder discovery, bool repeated = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<ChronicleOptions>>(Microsoft.Extensions.Options.Options.Create(new ChronicleOptions
        {
            Authentication = new Authentication { Enabled = true, EnforceAdministratorPolicyForOperations = true }
        }));
        services.AddSingleton(Substitute.For<IAdministratorPrivileges>());
        services.AddScoped<CallerMarker>();
        void RegisterCheck() => services.AddScoped<IAdministratorAuthorization>(provider =>
        {
            var check = new ScopedCheck(provider.GetRequiredService<CallerMarker>());
            _created.Add(check);
            return check;
        });

        if (registerBefore) RegisterCheck();
        if (discovery == DiscoveryOrder.BeforeExtension) Discover(services, includeConsumers: true);
        services.AddChronicleAdministratorAuthorization();
        if (!registerBefore) RegisterCheck();
        if (repeated) services.AddChronicleAdministratorAuthorization();
        if (discovery == DiscoveryOrder.AfterExtension) Discover(services, includeConsumers: true);
        if (discovery == DiscoveryOrder.None) Discover(services, includeConsumers: false);

        // Validation is deliberately not bypassed when a registration graph is invalid.
        await using var root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await using var first = root.CreateAsyncScope();
        await using var second = root.CreateAsyncScope();
        first.ServiceProvider.GetRequiredService<CallerMarker>().Name = "caller-one";
        second.ServiceProvider.GetRequiredService<CallerMarker>().Name = "caller-two";
        var firstCheck = (ScopedCheck)first.ServiceProvider.GetRequiredService<IAdministratorAuthorization>();
        var secondCheck = (ScopedCheck)second.ServiceProvider.GetRequiredService<IAdministratorAuthorization>();
        ReferenceEquals(firstCheck, secondCheck).ShouldBeFalse();

        await Exercise(first.ServiceProvider, firstCheck, secondCheck);
        await Exercise(second.ServiceProvider, secondCheck, firstCheck);
        await Exercise(first.ServiceProvider, firstCheck, secondCheck);

        // A root lookup must be rejected before invoking the scoped factory.
        Assert.Throws<InvalidOperationException>(root.GetRequiredService<IAdministratorAuthorization>);
        _created.Count.ShouldEqual(2);
        _created.Exists(check => check.Marker.Name == "root").ShouldBeFalse();
    }

    static void Discover(ServiceCollection services, bool includeConsumers)
    {
        // Run the real discovery extensions with the registrations already present at this point.
        // Copy only this security slice back, avoiding unrelated storage/Orleans startup dependencies.
        var discovered = new ServiceCollection();
        foreach (var descriptor in services) ((IServiceCollection)discovered).Add(descriptor);
        var existing = discovered.Count;
        discovered.AddTypeDiscovery();
        discovered.AddBindingsByConvention();
        discovered.AddSelfBindings();
        Type[] filters = [typeof(SystemStoreAuthorizationCommandFilter), typeof(SystemStoreQueryFilter)];
        Type[] consumers =
        [
            typeof(AdministratorAuthorization), typeof(AuthorizationDecisions), typeof(SystemStoreAccessVerdict),
            typeof(AdministratorAuthorizationHandler), typeof(SystemStoreOperationAuthorizationHandler)
        ];
        var selectedTypes = filters.Concat(includeConsumers ? consumers : []).ToHashSet();
        foreach (var descriptor in discovered.Skip(existing).Where(descriptor => IsSelected(descriptor, selectedTypes))) ((IServiceCollection)services).Add(descriptor);

        foreach (var filter in filters) services.Any(descriptor => descriptor.ServiceType == filter).ShouldBeTrue();
        if (includeConsumers)
        {
            foreach (var consumer in consumers)
            {
                services.Single(descriptor => descriptor.ServiceType == consumer).Lifetime.ShouldEqual(ServiceLifetime.Singleton);
            }
        }
    }

    static bool IsSelected(ServiceDescriptor descriptor, HashSet<Type> types) =>
        types.Contains(descriptor.ServiceType) || (descriptor.ImplementationType is { } implementation && types.Contains(implementation));

    static async Task Exercise(IServiceProvider services, ScopedCheck own, ScopedCheck other)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, own.Marker.Name)], "spec"));
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal, RequestServices = services };
        services.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().HttpContext = http;
        var decisions = services.GetRequiredService<IAuthorizationDecisions>();
        var access = services.GetRequiredService<ISystemStoreAccess>();
        var handlers = services.GetServices<IAuthorizationHandler>().ToArray();
        handlers.OfType<AdministratorAuthorizationHandler>().Count().ShouldEqual(1);
        handlers.OfType<SystemStoreOperationAuthorizationHandler>().Count().ShouldEqual(1);
        var commandFilter = services.GetRequiredService<SystemStoreAuthorizationCommandFilter>();
        var queryFilter = services.GetRequiredService<SystemStoreQueryFilter>();

        await UsesOwnCheck(async () => (await decisions.Decide(principal, WellKnownCapabilities.AdministerSecurity, AuthorizationScope.Kernel)).IsAllowed.ShouldBeTrue());
        await UsesOwnCheck(async () => (await access.IsAllowed(principal)).ShouldBeTrue());
        foreach (var handler in handlers.Where(handler => handler is AdministratorAuthorizationHandler or SystemStoreOperationAuthorizationHandler))
        {
            IAuthorizationRequirement requirement = handler is AdministratorAuthorizationHandler
                ? new AdministratorRequirement()
                : new SystemStoreOperationRequirement();
            var context = new AuthorizationHandlerContext([requirement], principal, new object());
            await UsesOwnCheck(() => handler.HandleAsync(context));
            context.HasSucceeded.ShouldBeTrue();
        }

        var command = new RetryPartition("System", "Default", "observer", "event-log", "partition");
        var commandContext = new CommandContext(CorrelationId.New(), command.GetType(), command, [], new CommandContextValues(), ServiceProvider: services);
        var queryContext = new QueryContext("Cratis.Chronicle.Security.LifetimeProbe", CorrelationId.New(), Paging.NotPaged, Sorting.None, new QueryArguments { ["EventStore"] = "System" }, ServiceProvider: services);

        // Arc's authorized caller normally comes from its pipeline; the accessor supplies it here.
        services.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().HttpContext = http;
        await UsesOwnCheck(async () => (await commandFilter.OnExecution(commandContext)).ShouldBeNull());
        await UsesOwnCheck(async () => (await queryFilter.OnPerform(queryContext)).ShouldBeNull());

        async Task UsesOwnCheck(Func<Task> operation)
        {
            var ownCalls = own.Callers.Count;
            var otherCalls = other.Callers.Count;
            await operation();
            own.Callers.Skip(ownCalls).ToArray().ShouldEqual([own.Marker.Name]);
            other.Callers.Count.ShouldEqual(otherCalls);
        }
    }

    [IgnoreConvention]
    public class CallerMarker
    {
        public string Name { get; set; } = "root";
    }

    [IgnoreConvention]
    class ScopedCheck(CallerMarker marker) : IAdministratorAuthorization
    {
        public CallerMarker Marker { get; } = marker;
        public List<string?> Callers { get; } = [];

        public Task<bool> IsAuthorized(ClaimsPrincipal principal)
        {
            Callers.Add(principal.Identity?.Name);
            return Task.FromResult(principal.Identity?.Name == Marker.Name);
        }
    }
}
