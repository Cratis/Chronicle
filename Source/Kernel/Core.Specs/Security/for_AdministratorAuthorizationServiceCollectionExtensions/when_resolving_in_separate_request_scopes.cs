// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

public class when_resolving_in_separate_request_scopes : given.the_kernel_authorization
{
    IAuthorizationDecisions _firstDecisions;
    IAuthorizationDecisions _secondDecisions;
    IAdministratorAuthorization _firstAdministrator;
    IAdministratorAuthorization _secondAdministrator;
    ISystemStoreAccess _firstAccess;
    ISystemStoreAccess _secondAccess;
    IAuthorizationHandler[] _firstHandlers;
    IAuthorizationHandler[] _secondHandlers;

    void Establish()
    {
        _services.AddLogging();
        _services.AddSingleton<IOptions<ChronicleOptions>>(Microsoft.Extensions.Options.Options.Create(new ChronicleOptions()));
        _services.AddSingleton(Substitute.For<IAdministratorPrivileges>());
        _services.AddChronicleAdministratorAuthorization();
        _provider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    async Task Because()
    {
        await using var first = _provider.CreateAsyncScope();
        await using var second = _provider.CreateAsyncScope();
        _firstDecisions = first.ServiceProvider.GetRequiredService<IAuthorizationDecisions>();
        _secondDecisions = second.ServiceProvider.GetRequiredService<IAuthorizationDecisions>();
        _firstAdministrator = first.ServiceProvider.GetRequiredService<IAdministratorAuthorization>();
        _secondAdministrator = second.ServiceProvider.GetRequiredService<IAdministratorAuthorization>();
        _firstAccess = first.ServiceProvider.GetRequiredService<ISystemStoreAccess>();
        _secondAccess = second.ServiceProvider.GetRequiredService<ISystemStoreAccess>();
        _firstHandlers = first.ServiceProvider.GetServices<IAuthorizationHandler>().Where(IsKernelHandler).ToArray();
        _secondHandlers = second.ServiceProvider.GetServices<IAuthorizationHandler>().Where(IsKernelHandler).ToArray();
    }

    [Fact] void should_share_the_decision_point() => ReferenceEquals(_firstDecisions, _secondDecisions).ShouldBeTrue();
    [Fact] void should_share_administrator_authorization() => ReferenceEquals(_firstAdministrator, _secondAdministrator).ShouldBeTrue();
    [Fact] void should_share_system_store_access() => ReferenceEquals(_firstAccess, _secondAccess).ShouldBeTrue();
    [Fact] void should_share_the_kernel_handlers() => _firstHandlers.SequenceEqual(_secondHandlers).ShouldBeTrue();
    [Fact] void should_register_both_kernel_handlers() => _firstHandlers.Length.ShouldEqual(2);

    static bool IsKernelHandler(IAuthorizationHandler handler) => handler is AdministratorAuthorizationHandler or SystemStoreOperationAuthorizationHandler;
}
