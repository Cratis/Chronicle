// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Application = Cratis.Chronicle.Storage.Security.Application;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService;

public class when_creating_the_development_client : Specification
{
    Application _application;
    ConfiguredGrant[] _grants;
    AuthenticationService _service;

    void Establish()
    {
        var applications = Substitute.For<IApplicationStorage>();
        applications.GetAll().Returns([]);
        applications.Create(Arg.Do<Application>(application => _application = application)).Returns(Task.CompletedTask);
        var grains = Substitute.For<IGrainFactory>();
        var entitlements = Substitute.For<IStoreEntitlements>();
        grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key).Returns(entitlements);
        entitlements.Reconcile(Arg.Do<IEnumerable<ConfiguredGrant>>(grants => _grants = grants.ToArray())).Returns(Task.CompletedTask);
        _service = new(Substitute.For<IUserStorage>(), applications, grains, Options.Create(new ChronicleOptions()), Substitute.For<IEventSerializer>(), Substitute.For<IInitialAdminSetupTokens>(), NullLogger<AuthenticationService>.Instance);
    }

    async Task Because() => await _service.EnsureDefaultClientCredentials();

    [Fact] void should_use_the_persisted_application_subject() => _grants.Single().Holder.ShouldEqual(new PrincipalKey(PrincipalKey.LocalIssuer, _application.Id.ToString()));
    [Fact] void should_use_an_ordinary_all_store_configuration_grant() => _grants.Single().ShouldEqual(new ConfiguredGrant(new(PrincipalKey.LocalIssuer, _application.Id.ToString()), null, null, StoreAccess.Read | StoreAccess.Append | StoreAccess.Manage));
}
#endif
