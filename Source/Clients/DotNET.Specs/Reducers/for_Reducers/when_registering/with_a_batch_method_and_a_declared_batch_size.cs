// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering;

public class with_a_batch_method_and_a_declared_batch_size : given.all_dependencies
{
    PageDelivery _pageDelivery;

    void Establish()
    {
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(Connections.ConnectionId.New());
        var invoker = Substitute.For<IReducerInvoker, IBatchReducerInvoker>();
        ((IBatchReducerInvoker)invoker).HasBatchMethod.Returns(true);
        var handler = Substitute.For<IReducerHandler>();
        handler.ReducerType.Returns(typeof(BatchReducer));
        handler.ReadModelType.Returns(typeof(BatchReadModel));
        handler.Invoker.Returns(invoker);
        handler.Id.Returns((ReducerId)"batch");
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        handler.EventTypes.Returns([new EventType("deposited", 1)]);
        handler.IsActive.Returns(true);
        _handlersByModelType[typeof(BatchReadModel)] = handler;
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            call.Arg<IObservable<ReducerMessage>>().Take(1).Subscribe(message => _pageDelivery = message.Content.Value0!.PageDelivery);
            return Observable.Never<ReduceOperationMessage>();
        });
    }

    async Task Because() => await _reducers.Register();

    [Fact] void should_declare_page_support() => _pageDelivery.Enabled.ShouldBeTrue();
    [Fact] void should_send_the_declared_max_bytes() => _pageDelivery.MaxBytesPerPage.ShouldEqual(2048U);
    [Fact] void should_send_the_batch_method_default_of_max_events() => _pageDelivery.MaxEventsPerPage.ShouldEqual(500U);
    [Fact] void should_send_no_delay() => _pageDelivery.MaxDelayMilliseconds.ShouldEqual(0U);

    [BatchSize(MaxBytes = 2048)]
    class BatchReducer;

    record BatchReadModel;
}
