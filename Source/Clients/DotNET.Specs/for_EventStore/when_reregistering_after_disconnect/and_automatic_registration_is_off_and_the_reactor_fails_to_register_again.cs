// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect;

public class and_automatic_registration_is_off_and_the_reactor_fails_to_register_again : when_preparing_for_registration.given.an_explicit_store
{
    readonly InvalidOperationException _failure = new("the kernel refused the registration");
    Exception _error;

    async Task Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        var activated = new ActivatedArtifact(new Classic(), typeof(Classic), NullLogger<ActivatedArtifact>.Instance);
        _activator.Activate(Arg.Any<IServiceProvider>(), typeof(Classic)).Returns(activated);
        _services.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()).Returns(Observable.Never<EventsToObserve>());
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(Observable.Never<ReduceOperationMessage>());
        _auto = false;
        _store = CreateStore();
        await _store.DiscoverAll();
        await _connection.Lifecycle.Connected();
        await _store.Reactors.Register("bridge", builder => builder.WithEventType(new EventType("orders", 1)), (_, _) => Task.CompletedTask);
        await _store.Reducers.Register(typeof(Classic));
        _services.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()).Returns(_ => throw _failure);
    }

    async Task Because()
    {
        await _connection.Lifecycle.Disconnected();
        _error = await Catch.Exception(_connection.Lifecycle.Connected);
    }

    void Destroy()
    {
        foreach (var handler in _store.Reducers.GetAllHandlers())
        {
            handler.Disconnect();
            (handler as IDisposable)?.Dispose();
        }
    }

    [Fact] void should_still_register_the_reducer_on_the_new_connection() => _services.Reducers.Received(2).Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>());
    [Fact] void should_carry_the_registration_failure() => ((AggregateException)_error).InnerExceptions.ShouldContainOnly(_failure);

    public record Model(string Name);

    public class Classic : IReducer
    {
        public Model Apply(PlainEvent @event, Model? current) => new(@event.Name);
    }
}
