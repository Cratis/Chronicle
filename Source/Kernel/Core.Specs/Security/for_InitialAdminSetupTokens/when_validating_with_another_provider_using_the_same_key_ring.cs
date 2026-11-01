// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_InitialAdminSetupTokens;

public class when_validating_with_another_provider_using_the_same_key_ring : given.setup_tokens
{
    ServiceProvider _first;
    ServiceProvider _second;

    void Establish()
    {
        var keys = new List<XElement>();
        var repository = Substitute.For<IXmlRepository>();
        repository.GetAllElements().Returns(_ => keys.ToArray());
        repository.When(_ => _.StoreElement(Arg.Any<XElement>(), Arg.Any<string>())).Do(call => keys.Add(call.Arg<XElement>()));
        _first = CreateProvider(repository);
        _token = new InitialAdminSetupTokens(_first.GetRequiredService<IDataProtectionProvider>(), Options.Create(new Configuration.ChronicleOptions())).Issue(_userId);
        _second = CreateProvider(repository);
        _tokens = new(_second.GetRequiredService<IDataProtectionProvider>(), Options.Create(new Configuration.ChronicleOptions()));
    }

    void Because() => _valid = _tokens.IsValid(_userId, _token);
    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
    }
    [Fact] void should_accept_the_token_from_another_instance() => _valid.ShouldBeTrue();

    static ServiceProvider CreateProvider(IXmlRepository repository)
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("Cratis.Chronicle").AddKeyManagementOptions(options => options.XmlRepository = repository);
        return services.BuildServiceProvider();
    }
}
