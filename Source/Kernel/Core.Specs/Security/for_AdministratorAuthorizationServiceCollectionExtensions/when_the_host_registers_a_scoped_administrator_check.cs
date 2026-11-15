// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorizationServiceCollectionExtensions;

public class when_the_host_registers_a_scoped_administrator_check : given.the_kernel_authorization
{
    IAuthorizationDecisions _firstDecisions;
    IAuthorizationDecisions _secondDecisions;
    IAdministratorAuthorization _firstAdministrator;
    IAdministratorAuthorization _secondAdministrator;

    void Establish()
    {
        _services.AddLogging();
        _services.AddSingleton<IOptions<ChronicleOptions>>(Microsoft.Extensions.Options.Options.Create(new ChronicleOptions()));
        _services.AddScoped(_ => Substitute.For<IAdministratorAuthorization>());
        _services.AddChronicleAdministratorAuthorization();
        _provider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    void Because()
    {
        using var first = _provider.CreateScope();
        using var second = _provider.CreateScope();
        _firstDecisions = first.ServiceProvider.GetRequiredService<IAuthorizationDecisions>();
        _secondDecisions = second.ServiceProvider.GetRequiredService<IAuthorizationDecisions>();
        _firstAdministrator = first.ServiceProvider.GetRequiredService<IAdministratorAuthorization>();
        _secondAdministrator = second.ServiceProvider.GetRequiredService<IAdministratorAuthorization>();
    }

    [Fact] void should_share_the_singleton_decisions_between_requests() => ReferenceEquals(_firstDecisions, _secondDecisions).ShouldBeTrue();
    [Fact] void should_keep_the_hosts_check_per_request() => ReferenceEquals(_firstAdministrator, _secondAdministrator).ShouldBeFalse();
}
