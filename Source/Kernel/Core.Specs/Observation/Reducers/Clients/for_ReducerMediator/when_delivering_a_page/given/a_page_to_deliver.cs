// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerMediator.when_delivering_a_page.given;

public class a_page_to_deliver : for_ReducerMediator.given.a_reducer_mediator
{
    protected static readonly ReducerId _reducer = new("reducer");
    protected static readonly ConnectionId _connection = new("connection");
    protected static readonly EventStoreName _store = new("store");
    protected static readonly EventStoreNamespaceName _namespace = new("namespace");

    protected readonly List<ReduceOperation> _singleDeliveries = [];
    protected readonly List<IReadOnlyList<ReduceOperation>> _pageDeliveries = [];
    protected IReadOnlyList<ReduceOperation> _operations;
    protected Dictionary<Key, TaskCompletionSource<ReducerSubscriberResult>> _completions;

    void Establish()
    {
        _operations = [new((Key)"first", [], null), new((Key)"second", [], null)];
        _completions = _operations.ToDictionary(_ => _.Partition, _ => new TaskCompletionSource<ReducerSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously));
    }

    protected void SubscribeEvents() => _mediator.Subscribe(_reducer, _connection, _store, _namespace, (operation, _) => _singleDeliveries.Add(operation));

    protected void SubscribePages() => _mediator.SubscribePages(_reducer, _connection, _store, _namespace, (operations, _) => _pageDeliveries.Add(operations));

    protected void DeliverThePage() => _mediator.OnNextPage(_reducer, _connection, _store, _namespace, _operations, _completions);
}
