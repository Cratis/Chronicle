// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Setup.Authentication;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_an_existing_username_matches_bootstrap_configuration : given.an_administrator_roster
{
    IAdministratorRoster _coordinator;
    AuthenticationService _authentication;

    async Task Establish()
    {
        await AddUser(_first, "root");
        _coordinator = Substitute.For<IAdministratorRoster>();
        _grainFactory.GetGrain<IAdministratorRoster>(Concepts.Security.AdministratorRoster.Key).Returns(_coordinator);
        _authentication = new AuthenticationService(
            _storage.System.Users,
            Substitute.For<IApplicationStorage>(),
            _grainFactory,
            Options.Create(_options),
            _serializer,
            Substitute.For<IInitialAdminSetupTokens>(),
            NullLogger<AuthenticationService>.Instance);
    }

    async Task Because() => await _authentication.EnsureDefaultAdminUser();

    [Fact] void should_not_promote_the_same_named_user() => _coordinator.DidNotReceive().Grant(Arg.Any<UserId>(), Arg.Any<GrantReason>(), Arg.Any<string>());
}
