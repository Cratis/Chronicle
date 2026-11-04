// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Releases;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors;

public class when_registering_at_startup : Specification
{
    Reactors _reactors;
    IObserver _observer;
    ObserverKey _key;
    ReactorDefinition _definition;

    void Establish()
    {
        var types = Substitute.For<ITypes>();
        types.FindMultiple<IReactor>().Returns([typeof(UsersReactor)]);
        var storage = Substitute.For<IStorage>();
        var system = Substitute.For<IEventStoreStorage>();
        var definitions = Substitute.For<IReactorDefinitionsStorage>();
        definitions.Has(Arg.Any<ReactorId>()).Returns(false);
        definitions.Save(Arg.Any<ReactorDefinition>()).Returns(call =>
        {
            _definition = call.Arg<ReactorDefinition>();
            return Task.CompletedTask;
        });
        system.Reactors.Returns(definitions);
        storage.GetEventStore(EventStoreName.System).Returns(system);
        var grainFactory = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        grainFactory.GetGrain<IObserver>(Arg.Any<string>()).Returns(call =>
        {
            _key = ObserverKey.Parse(call.ArgAt<string>(0));
            return _observer;
        });
        var releases = Substitute.For<IClusterReleases>();
        releases.Summarize().Returns(new ClusterReleaseSummary([], 0, 0));
        _reactors = new(types, Substitute.For<ILocalSiloDetails>(), storage, grainFactory, releases, Substitute.For<ILogger<Reactors>>());
    }

    async Task Because() => await _reactors.DiscoverAndRegister(EventStoreName.System, EventStoreNamespaceName.Default);

    async Task Destroy() => await _reactors.DisposeAsync();

    [Fact] void should_address_the_system_observer() => _key.ShouldEqual(new ObserverKey($"$system.{typeof(UsersReactor).GetReactorId()}", EventStoreName.System, EventStoreNamespaceName.Default, EventSequenceId.Log));
    [Fact] void should_persist_a_kernel_definition() => _definition.Owner.ShouldEqual(ReactorOwner.Kernel);

    [Fact]
    void should_subscribe_the_kernel_subscriber_without_marking_the_subscription_automatic()
    {
        var subscribe = _observer.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IObserver.Subscribe));
        subscribe.GetMethodInfo().GetGenericArguments().Single().ShouldEqual(typeof(IReactorObserverSubscriber<UsersReactor>));
        var parameters = subscribe.GetMethodInfo().GetParameters();
        var automatic = Array.FindIndex(parameters, parameter => parameter.Name == "automatic");
        ((bool)subscribe.GetArguments()[automatic]!).ShouldBeFalse();
    }

    [Fact]
    void should_not_depend_on_any_access_guard()
    {
        var dependencies = typeof(Reactors).GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        dependencies.ShouldNotContain(typeof(ISystemStoreAccess));
        dependencies.ShouldNotContain(typeof(IAdministratorAuthorization));
        dependencies.ShouldNotContain(typeof(IHttpContextAccessor));
    }
}
