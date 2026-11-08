// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using NSubstitute.ClearExtensions;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_a_page.given;

public class a_page_subscriber : for_ReducerObserverSubscriber.given.a_subscriber_with_a_cached_pipeline
{
    protected readonly Dictionary<string, ReducerSubscriberResult> _clientResults = new()
    {
        ["first"] = new(ObserverSubscriberResult.Ok(1UL), new ExpandoObject()),
        ["second"] = new(ObserverSubscriberResult.Ok(2UL), new ExpandoObject())
    };

    protected readonly Dictionary<Key, ReadModelChangeNotification> _notifications = [];
    protected readonly List<FailedPartition> _writeFailures = [];
    protected readonly List<IReadOnlyList<ReduceOperation>> _pagesSentToTheClient = [];
    protected ObserverPageResult _result;

    protected static IReadOnlyList<ObserverPartitionBatch> Batches =>
    [
        new((Key)"first", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("the-event", 1), 1UL)]),
        new((Key)"second", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("the-event", 1), 2UL)])
    ];

    void Establish()
    {
        _mediator.ClearSubstitute();
        _mediator.When(_ => _.OnNextPage(
                Arg.Any<ReducerId>(),
                Arg.Any<ConnectionId>(),
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Any<IReadOnlyList<ReduceOperation>>(),
                Arg.Any<IReadOnlyDictionary<Key, TaskCompletionSource<ReducerSubscriberResult>>>()))
            .Do(call =>
            {
                var operations = call.Arg<IReadOnlyList<ReduceOperation>>();
                _pagesSentToTheClient.Add(operations);
                foreach (var (key, completion) in call.Arg<IReadOnlyDictionary<Key, TaskCompletionSource<ReducerSubscriberResult>>>())
                {
                    completion.SetResult(_clientResults[key.Value.ToString()!]);
                }
            });

        _firstPipeline.ReducePage(Arg.Any<IReadOnlyList<ReducerContext>>(), Arg.Any<ReducerPageDelegate>(), Arg.Any<bool>()).Returns(async call =>
        {
            var contexts = call.Arg<IReadOnlyList<ReducerContext>>();
            var results = await call.Arg<ReducerPageDelegate>()([.. contexts.Select(_ => new ReducerPagePartition(_.Key, _.Events, null))]);
            return new ReducerPageOutcome(results, _notifications, _writeFailures);
        });
    }

    protected static ReadModelChangeNotification ChangeFor(string partition) =>
        new(partition, new JsonObject(), new ReadModelChangeContext(ReadModelChangeType.Modified, 1UL, DateTimeOffset.UtcNow, CorrelationId.NotSet));

    protected async Task Deliver() => _result = await _subscriber.OnNextPartitions(Batches, new ObserverSubscriberContext(new ConnectedClient { ConnectionId = "connection" }));

    protected ObserverSubscriberResult ResultFor(string partition) => _result.Results[(Key)partition];
}
