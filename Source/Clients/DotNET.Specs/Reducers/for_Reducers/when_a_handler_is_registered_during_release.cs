// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_a_handler_is_registered_during_release : given.two_discovered_reducers
{
    CancellationTokenRegistration _registration;
    IReducerHandler _added;

    void Establish()
    {
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(ConnectionId.New());
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(Observable.Never<ReduceOperationMessage>());
        _registration = _firstToken.Register(() => _added = _reducers.Register(typeof(Added)).GetAwaiter().GetResult());
    }

    void Because() => _reducers.Release();

    void Destroy() => _registration.Dispose();

    [Fact] void should_remove_the_released_cohort() => _reducers.GetAllHandlers().ShouldContainOnly(_added);
    [Fact] void should_preserve_the_callback_added_type() => _reducers.GetHandlerFor(typeof(Added)).ShouldBeSame(_added);
    [Fact] void should_preserve_the_callback_added_model() => _reducers.HasFor<Added>().ShouldBeTrue();
    [Fact] void should_not_cancel_the_callback_added_handler() => _added.CancellationToken.IsCancellationRequested.ShouldBeFalse();
    [Fact] void should_still_cancel_the_original_handlers() => (_firstToken.IsCancellationRequested && _secondToken.IsCancellationRequested).ShouldBeTrue();

    public record Added
    {
        public static Added Reduce(Changed @event, Added? current) => current ?? new();
    }
}
