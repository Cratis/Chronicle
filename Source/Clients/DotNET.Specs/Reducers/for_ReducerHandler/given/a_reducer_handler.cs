// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reducers.for_ReducerHandler.given;

public class a_reducer_handler : Specification
{
    protected IEventStore _eventStore;
    protected ReducerId _reducerId;
    protected Type _reducerType;
    protected EventSequenceId _eventSequenceId;
    protected IReducerInvoker _invoker;
    protected IReducerObservers _reducerObservers;
    protected ReducerHandler _handler;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Name.Returns((EventStoreName)"test-event-store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"test-namespace");

        _reducerId = "reducer-id";
        _reducerType = typeof(object);
        _eventSequenceId = EventSequenceId.Log;
        _invoker = Substitute.For<IReducerInvoker>();
        _reducerObservers = Substitute.For<IReducerObservers>();

        _handler = new ReducerHandler(
            _eventStore,
            _reducerId,
            _reducerType,
            _eventSequenceId,
            _invoker,
            true,
            _reducerObservers);
    }
}
