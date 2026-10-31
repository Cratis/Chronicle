// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService.given;

public class an_existing_administrator : Specification
{
    protected Storage.Security.User _administrator;
    protected IUserStorage _users;
    protected IInitialAdminSetupTokens _tokens;
    protected setup_logger _logger;
    private protected AuthenticationService _service;

    void Establish()
    {
        _administrator = new() { Id = UserId.InitialAdministrator, Username = "admin", HasLoggedIn = false };
        _users = Substitute.For<IUserStorage>();
        _users.GetAll().Returns([_administrator]);
        _tokens = Substitute.For<IInitialAdminSetupTokens>();
        _tokens.Issue(_administrator.Id).Returns((InitialAdminSetupToken)"issued-token");
        _logger = new();
        var factory = Substitute.For<ILoggerFactory>();
        factory.CreateLogger(Arg.Any<string>()).Returns(_logger.Logger);
        _service = new(_users, Substitute.For<IApplicationStorage>(), Substitute.For<IGrainFactory>(), Options.Create(new Configuration.ChronicleOptions()), Substitute.For<IEventSerializer>(), _tokens, new Logger<AuthenticationService>(factory));
    }
}
