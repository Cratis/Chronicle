// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reactors.SideEffects.for_ReactorSideEffectHandlers;

public class when_a_handler_is_registered_by_interface_and_class : Specification
{
    ServiceProvider _provider;
    IReactorSideEffectHandler[] _handlers;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CustomHandler>();
        services.AddSingleton<IReactorSideEffectHandler>(provider => provider.GetRequiredService<CustomHandler>());
        _provider = services.BuildServiceProvider();
    }

    void Because() => _handlers = new EventStoreReactorSideEffectHandlerInstances(_provider).ToArray();
    void Destroy() => _provider.Dispose();

    [Fact] void should_resolve_the_custom_handler_only_once() => _handlers.Count(handler => handler is CustomHandler).ShouldEqual(1);

    public class CustomHandler : IReactorSideEffectHandler
    {
        public bool CanHandle(ReactorContext reactorContext, object value) => false;
        public Task<Result<ReactorSideEffectFailure>> Handle(ReactorContext reactorContext, IEventStore eventStore, object value) =>
            Task.FromResult(Result.Success<ReactorSideEffectFailure>());
    }
}
