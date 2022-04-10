// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.Orleans.Execution;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Core;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer;

public class when_something : Specification
{
    Observer observer;
    Mock<IRequestContextManager> request_context_manager;

    async Task Establish()
    {
        request_context_manager = new();

        observer = new Observer(request_context_manager.Object, Mock.Of<ILogger<Observer>>());

        var identityField = typeof(Grain).GetField("Identity", BindingFlags.Instance | BindingFlags.NonPublic);
        var grainIdentity = new Mock<IGrainIdentity>();
        identityField.SetValue(observer, grainIdentity.Object);

        var key = new ObserverKey(MicroserviceId.Unspecified, TenantId.Development, EventSequenceId.Log).ToString();
        grainIdentity.Setup(_ => _.GetPrimaryKey(out key)).Returns(Guid.NewGuid());

        var runtimeProperty = typeof(Grain).GetProperty("Runtime", BindingFlags.Instance | BindingFlags.NonPublic);
        var runtimeMock = new Mock<IGrainRuntime>();
        runtimeProperty.SetValue(observer, runtimeMock.Object);

        var serviceProviderMock = new Mock<IServiceProvider>();
        runtimeMock.SetupGet(_ => _.ServiceProvider).Returns(serviceProviderMock.Object);
        var streamProviderMock = new Mock<IStreamProvider>();

        var collectionMock = new Mock<IKeyedServiceCollection<string, IStreamProvider>>();
        serviceProviderMock.Setup(_ => _.GetService(typeof(IKeyedServiceCollection<string, IStreamProvider>))).Returns(collectionMock.Object);

        collectionMock.Setup(_ => _.GetService(serviceProviderMock.Object, WellKnownProviders.EventSequenceStreamProvider)).Returns(streamProviderMock.Object);

        var storageProperty = typeof(Grain<ObserverState>).GetField("storage", BindingFlags.Instance | BindingFlags.NonPublic);
        var storageProvider = new Mock<IStorage<ObserverState>>();
        storageProperty.SetValue(observer, storageProvider.Object);

        var state = new ObserverState();
        storageProvider.SetupGet(_ => _.State).Returns(state);

        await observer.OnActivateAsync();
    }

    async Task Because() => await observer.Rewind();

    [Fact] void should_do_stuff() => observer.ShouldNotBeNull();
}
