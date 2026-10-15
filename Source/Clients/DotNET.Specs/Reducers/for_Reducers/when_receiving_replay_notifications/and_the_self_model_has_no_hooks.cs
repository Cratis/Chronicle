// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_receiving_replay_notifications;

public class and_the_self_model_has_no_hooks : for_Reducers.given.all_dependencies
{
    async Task Because()
    {
        var handler = Substitute.For<IReducerHandler>();
        var invoker = new SelfReducerInvoker(RegisteredEventTypesForSpecifications.Create((typeof(Created), new("created", 1))), typeof(State), "state");
        handler.Invoker.Returns(invoker);
        var notify = typeof(Reducers).GetMethod("HandleReplayNotification", BindingFlags.Instance | BindingFlags.NonPublic)!;
        foreach (var state in new[] { ReplayState.BeginReplay, ReplayState.EndReplay, ReplayState.BeginReplayPartition, ReplayState.EndReplayPartition })
        {
            await (Task)notify.Invoke(_reducers, [handler, state, "one"])!;
        }
    }

    [Fact] void should_not_even_create_a_service_scope() => _serviceProvider.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_never_activate_the_self_model() => _artifactActivator.ReceivedCalls().ShouldBeEmpty();

    public record Created;
    public record State
    {
        public static State Reduce(Created @event, State? current) => new();
    }
}
