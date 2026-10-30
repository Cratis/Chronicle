// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_ChronicleDataProtection;

public class when_registering_repeatedly : Specification
{
    ServiceProvider _services;
    bool _valid;
    IXmlRepository _repository;

    void Establish()
    {
        var keys = new Dictionary<string, string>();
        var grain = Substitute.For<IDataProtectionKeys>();
        grain.GetAllKeys().Returns(_ => Task.FromResult<IEnumerable<string>>(keys.Values.ToArray()));
        grain.StoreKey(Arg.Any<string>(), Arg.Any<string>()).Returns(call =>
        {
            keys[call.ArgAt<string>(0)] = call.ArgAt<string>(1);
            return Task.CompletedTask;
        });
        var factory = Substitute.For<IGrainFactory>();
        factory.GetGrain<IDataProtectionKeys>("chronicle-data-protection").Returns(grain);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(factory);
        services.AddSingleton(Options.Create(new ChronicleOptions { Authentication = new Authentication { Enabled = false } }));
        services.AddChronicleDataProtection();
        services.AddChronicleDataProtection();
        _services = services.BuildServiceProvider();
    }

    void Because()
    {
        var tokens = ActivatorUtilities.CreateInstance<InitialAdminSetupTokens>(_services);
        var user = new UserId(Guid.NewGuid());
        _valid = tokens.IsValid(user, tokens.Issue(user));
        _repository = _services.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository;
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_issue_a_valid_token_with_authentication_disabled() => _valid.ShouldBeTrue();
    [Fact] void should_use_the_grain_repository() => _repository.ShouldBeSame(_services.GetRequiredService<GrainBasedXmlRepository>());
    [Fact] void should_have_one_repository() => _services.GetServices<IXmlRepository>().Count().ShouldEqual(1);
}
