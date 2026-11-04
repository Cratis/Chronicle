// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.when_registering;

public class and_the_client_reconnects : given.a_registration
{
    readonly TaskCompletionSource _secondRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _releases;

    void Establish() => _lease.When(_ => _.Dispose()).Do(_ =>
    {
        if (Interlocked.Increment(ref _releases) == 2)
        {
            _secondRelease.TrySetResult();
        }
    });

    async Task Because()
    {
        Connect();
        await _released.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Disconnect();
        Connect("reconnected");
        await _secondRelease.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] async Task should_acquire_a_permit_for_each_registration() => await _gate.Received(2).Enter(Arg.Any<CancellationToken>());
    [Fact] void should_release_each_lease_without_waiting_for_the_stream_to_close() => _lease.Received(2).Dispose();
    [Fact] void should_subscribe_again() => _client.Received(2).SetDefinitionAndSubscribe(Arg.Any<ReactorDefinition>());
}
