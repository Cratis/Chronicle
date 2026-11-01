// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching;

public class and_the_self_model_has_no_hooks : given.a_reducer_dispatcher
{
    void Establish()
    {
        var invoker = new SelfReducerInvoker(RegisteredEventTypesForSpecifications.Create((typeof(Created), new("created", 1))), typeof(State), "state");
        _handler.Invoker.Returns(invoker);
    }

    async Task Because()
    {
        foreach (var state in new[] { ReplayState.BeginReplay, ReplayState.EndReplay, ReplayState.BeginReplayPartition, ReplayState.EndReplayPartition })
        {
            await _dispatcher.Dispatch(_handler, new() { ReplayState = state, Partition = "one" }, "ns", Publish);
        }
    }

    [Fact] void should_not_even_create_a_service_scope() => _serviceProvider.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_never_activate_the_self_model() => _artifactActivator.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_publish_a_result() => _published.ShouldBeEmpty();

    public record Created;

    public new record State
    {
        public static State Reduce(Created @event, State? current) => new();
    }
}
