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

public class when_the_development_client_create_fails_and_nothing_is_stored : Specification
{
    readonly InvalidOperationException _failure = new("storage unavailable");
    AuthenticationService _service;
    Exception _error;

    void Establish()
    {
        var applications = Substitute.For<IApplicationStorage>();
        applications.GetAll().Returns([]);
        applications.Create(Arg.Any<Application>()).Returns(Task.FromException(_failure));
        _service = new(Substitute.For<IUserStorage>(), applications, Substitute.For<IGrainFactory>(), Options.Create(new ChronicleOptions()), Substitute.For<IEventSerializer>(), Substitute.For<IInitialAdminSetupTokens>(), NullLogger<AuthenticationService>.Instance);
    }

    async Task Because() => _error = await Catch.Exception(_service.EnsureDefaultClientCredentials);

    [Fact] void should_rethrow_the_failure() => _error.ShouldEqual(_failure);
}
#endif
