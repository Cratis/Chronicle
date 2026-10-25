// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching;

public class and_a_replay_begins_and_ends : given.a_reducer_dispatcher
{
    readonly List<string> _notifications = [];

    void Establish()
    {
        _handler.Invoker.Returns(Substitute.For<IReducerInvoker>());
        _artifactActivator.Activate(Arg.Any<IServiceProvider>(), typeof(object)).Returns(
            new ActivatedArtifact(new NotifiedReducer(_notifications), typeof(NotifiedReducer), Substitute.For<ILogger<ActivatedArtifact>>()));
    }

    async Task Because()
    {
        await _dispatcher.Dispatch(_handler, new() { ReplayState = ReplayState.BeginReplay }, "ns", Publish);
        await _dispatcher.Dispatch(_handler, new() { ReplayState = ReplayState.EndReplay }, "ns", Publish);
    }

    [Fact] void should_notify_the_replay_began_then_ended() => _notifications.ShouldContainOnly("begin", "end");
    [Fact] void should_not_publish_a_result() => _published.ShouldBeEmpty();
    [Fact] async Task should_not_reduce_any_events() => await _handler.DidNotReceive().OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>());

    class NotifiedReducer(List<string> notifications) : ICanBeNotifiedWhenReplay
    {
        public Task BeginReplay()
        {
            notifications.Add("begin");
            return Task.CompletedTask;
        }

        public Task EndReplay()
        {
            notifications.Add("end");
            return Task.CompletedTask;
        }
    }
}
