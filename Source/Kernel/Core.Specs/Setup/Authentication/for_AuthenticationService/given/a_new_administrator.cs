// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService.given;

public class a_new_administrator : Specification
{
    protected Configuration.Authentication _authentication = new();
    protected IInitialAdminSetupTokens _tokens;
    protected setup_logger _logger;
    protected Storage.Security.User _administrator;
    IUserStorage _users;
    IGrainFactory _grains;
    IEventSerializer _serializer;

    void Establish()
    {
        _administrator = new() { Id = UserId.InitialAdministrator, Username = "admin" };
        _users = Substitute.For<IUserStorage>();
        _users.GetAll().Returns([]);
        _users.GetById(_administrator.Id).Returns(_administrator);
        _serializer = Substitute.For<IEventSerializer>();
        _serializer.Serialize(Arg.Any<object>()).Returns(call =>
        {
            if (call.Arg<object>() is UserPasswordChanged changed)
            {
                _administrator.PasswordHash = changed.PasswordHash;
                _administrator.HasLoggedIn = true;
            }
            return new JsonObject();
        });
        var eventLog = Substitute.For<IEventSequence>();
        eventLog.Append(default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default!)
            .ReturnsForAnyArgs(AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));
        _grains = Substitute.For<IGrainFactory>();
        _grains.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(eventLog);
        _tokens = Substitute.For<IInitialAdminSetupTokens>();
        _tokens.Issue(_administrator.Id).Returns((InitialAdminSetupToken)"issued-token");
        _logger = new();
    }

    protected Task Bootstrap()
    {
        var factory = Substitute.For<ILoggerFactory>();
        factory.CreateLogger(Arg.Any<string>()).Returns(_logger.Logger);
        return new AuthenticationService(_users, Substitute.For<IApplicationStorage>(), _grains, Options.Create(new Configuration.ChronicleOptions { Authentication = _authentication }), _serializer, _tokens, new Logger<AuthenticationService>(factory)).EnsureDefaultAdminUser();
    }
}
