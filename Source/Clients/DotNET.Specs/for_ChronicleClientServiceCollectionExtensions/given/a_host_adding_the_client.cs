// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions.given;

public class a_host_adding_the_client : Specification
{
    protected ServiceCollection _services;
    protected ChronicleOptions _chronicleOptions;
    protected IEventTypes _eventTypes;
    protected IChronicleClient _client;

    void Establish()
    {
        _chronicleOptions = new ChronicleOptions();
        _eventTypes = Substitute.For<IEventTypes>();
        _services = new ServiceCollection();
        _client = Substitute.For<IChronicleClient>();
        _client.Options.Returns(_chronicleOptions);
    }

    protected ServiceProvider BuildProvider()
    {
        _services.AddSingleton(Options.Create(new ChronicleClientOptions()));
        _services.AddSingleton(Options.Create(_chronicleOptions));
        _services.AddLogging();
        _services.AddCratisChronicleClient();
        _services.AddSingleton(_client);
        _services.AddScoped(_ => _eventTypes);
        return _services.BuildServiceProvider();
    }
}
