// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_the_server_composes_the_authorization_graph : Specification
{
    [Fact]
    public async Task should_keep_the_caller_check_out_of_singleton_constructors()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Host.UseOrleans(silo => silo.AddChronicleToSilo().AddChronicleServicesAsInMemory());
        builder.Services.AddBindingsByConvention().AddSelfBindings();
        builder.Services.AddChronicleAuthentication(new Configuration.ChronicleOptions
        {
            Authentication = new Configuration.Authentication { Enabled = true, Authority = "https://identity.example" }
        });
        Type[] graph =
        [
            typeof(AdministratorAuthorization), typeof(AuthorizationDecisions), typeof(SystemStoreAccessVerdict),
            typeof(AdministratorAuthorizationHandler), typeof(SystemStoreOperationAuthorizationHandler),
            typeof(SystemStoreAuthorizationCommandFilter), typeof(SystemStoreCommandFilter),
            typeof(StoreEntitlementCommandFilter), typeof(SystemStoreQueryFilter)
        ];
        foreach (var type in graph)
        {
            var registrations = builder.Services.Where(descriptor => descriptor.ServiceType == type || (!descriptor.IsKeyedService && descriptor.ImplementationType == type)).ToArray();
            registrations.ShouldNotBeEmpty();
            registrations.All(descriptor => descriptor.Lifetime == ServiceLifetime.Singleton).ShouldBeTrue();
            type.GetConstructors().SelectMany(constructor => constructor.GetParameters()).Any(parameter => parameter.ParameterType == typeof(IAdministratorAuthorization)).ShouldBeFalse();
        }

        // Validate the composed authorization slice without starting storage, certificates, cluster membership or listeners.
        var slice = new ServiceCollection().AddLogging();
        var contracts = graph.Concat([typeof(IAuthorizationDecisions), typeof(ISystemStoreAccess), typeof(IAuthorizationHandler), typeof(Microsoft.AspNetCore.Http.IHttpContextAccessor)]).ToHashSet();
        foreach (var descriptor in builder.Services.Where(descriptor => contracts.Contains(descriptor.ServiceType))) slice.Add(descriptor);
        slice.AddSingleton(Microsoft.Extensions.Options.Options.Create(new Configuration.ChronicleOptions()));
        slice.AddScoped(_ => Substitute.For<IAdministratorAuthorization>());
        slice.AddSingleton(Substitute.For<IAdministratorPrivileges>());
        await using var provider = slice.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        provider.GetRequiredService<IAuthorizationDecisions>().ShouldNotBeNull();
        provider.GetServices<IAuthorizationHandler>().OfType<AdministratorAuthorizationHandler>().Count().ShouldEqual(1);
    }
}
