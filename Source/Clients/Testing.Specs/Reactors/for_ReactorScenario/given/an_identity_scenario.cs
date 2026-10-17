// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

public class an_identity_scenario : Specification
{
    protected Defaults _defaults;
    protected ReactorScenario<IdentityReactor> _scenario;
    protected IEventStore _usedStore;
    protected IReactorSideEffectHandlers _handlers;
    protected ContextRecorder _recorder;
    protected readonly InMemoryClientArtifactsProvider _artifacts = new() { EventTypes = [typeof(ReactorIdentityRecorded)], Reactors = [typeof(IdentityReactor)] };

    void Establish()
    {
        _defaults = new(_artifacts, null, null, null, "reactor-store", "reactor-namespace");
        _recorder = new();
        _handlers = Substitute.For<IReactorSideEffectHandlers>();
        _handlers.CanHandleReturnType(Arg.Any<Type>()).Returns(true);
        _handlers.CanHandle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>()).Returns(true);
        _handlers.Handle(Arg.Any<ReactorContext>(), Arg.Any<IEventStore>(), Arg.Any<object>()).Returns(call =>
        {
            _usedStore = call.Arg<IEventStore>();
            return new EventResultHandler().Handle(call.Arg<ReactorContext>(), _usedStore, call.ArgAt<object>(2));
        });
    }

    protected void RegisterRecorder() => _scenario.Services.AddSingleton(_recorder);

    void Destroy()
    {
        _scenario.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }

    [EventType]
    public record ReactorIdentityRecorded;

    public class ContextRecorder
    {
        public EventContext Context { get; set; } = EventContext.Empty;
    }

    public class IdentityReactor(ContextRecorder recorder) : IReactor
    {
        public ReactorIdentityRecorded Handle(ReactorIdentityRecorded @event, EventContext context)
        {
            recorder.Context = context;
            return new ReactorIdentityRecorded();
        }
    }
}
