// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.when_registering;

public class and_the_client_disconnects_while_waiting : given.a_registration
{
    readonly CancellationTokenSource _cancellation = new();

    void Establish() => ExhaustGate();

    async Task Because()
    {
        Connect(cancellationToken: _cancellation.Token);
        await _entering.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _cancellation.Cancel();
        await _cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_not_start_a_read_model_lookup() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_the_definition() => _readModel.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_release_a_permit_it_did_not_acquire() => _lease.DidNotReceive().Dispose();
}
