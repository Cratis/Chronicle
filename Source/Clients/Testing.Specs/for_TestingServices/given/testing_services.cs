// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Testing.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.for_TestingServices.given;

public class testing_services : Specification
{
    protected IServices _services;
    ServiceProvider _serviceProvider;
    EventStoreForTesting _eventStore;

    void Establish()
    {
        _eventStore = new EventStoreForTesting(null, Substitute.For<IClientArtifactsProvider>());
        var services = new ServiceCollection();
        services.AddCratisChronicleConnection();
        services.AddSingleton(_eventStore.Connection);
        _serviceProvider = services.BuildServiceProvider();
        _services = _serviceProvider.GetRequiredService<IServices>();
    }

    void Destroy()
    {
        _serviceProvider.Dispose();
        _eventStore.Connection.Dispose();
    }
}
