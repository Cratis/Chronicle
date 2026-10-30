// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching;

public class and_the_handler_targets_a_newer_generation : given.a_reducer_dispatcher
{
    AppendedEvent _received;

    void Establish()
    {
        _handler.EventTypes.Returns([new EventType("something-happened", 2)]);
        _handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>()).Returns(call =>
        {
            _received = call.Arg<IEnumerable<AppendedEvent>>().Single();
            return new ReduceResult(null, 1, [], string.Empty);
        });
    }

    async Task Because()
    {
        var operation = OperationWith(1);
        operation.Events[0].Content = "{\"name\":\"old\"}";
        operation.Events[0].GenerationalContent = new Dictionary<int, string> { [2] = "{\"name\":\"new\"}" };
        await _dispatcher.Dispatch(_handler, operation, "ns", Publish);
    }

    [Fact] void should_deserialize_the_content_of_the_targeted_generation() => ((SomethingHappened)_received.Content).Name.ShouldEqual("new");
    [Fact] void should_rewrite_the_context_generation() => _received.Context.EventType.Generation.Value.ShouldEqual(2U);
}
