// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.when_registering;

public class and_subscribing_times_out_while_the_observer_is_busy : given.a_registration
{
    readonly TaskCompletionSource _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _attempts;

    void Establish() => _client.SetDefinitionAndSubscribe(Arg.Any<ReactorDefinition>()).Returns(_ =>
    {
        if (++_attempts == 1)
        {
            return Task.FromException(new TimeoutException("Response did not arrive on time"));
        }

        _subscribed.TrySetResult();
        return Task.CompletedTask;
    });

    async Task Because()
    {
        Connect();
        await _subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_ask_again_on_the_same_stream() => _attempts.ShouldEqual(2);
    [Fact] void should_keep_the_stream_open() => _error.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_unsubscribe_the_reactor() => _client.DidNotReceive().Unsubscribe();
}
