// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.when_registering;

public class and_the_registration_gate_is_full : given.a_registration
{
    bool _lookupWasBlocked;
    bool _definitionWasBlocked;
    bool _subscriptionWasBlocked;

    void Establish() => ExhaustGate();

    async Task Because()
    {
        Connect();
        await _entering.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _lookupWasBlocked = !_grainFactory.ReceivedCalls().Any();
        _definitionWasBlocked = !_readModel.ReceivedCalls().Any();
        _subscriptionWasBlocked = !_client.ReceivedCalls().Any();
        _permission.SetResult(_lease);
        await _released.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_block_the_first_read_model_lookup() => _lookupWasBlocked.ShouldBeTrue();
    [Fact] void should_not_read_the_definition_without_a_permit() => _definitionWasBlocked.ShouldBeTrue();
    [Fact] void should_not_subscribe_until_a_permit_is_available() => _subscriptionWasBlocked.ShouldBeTrue();
    [Fact] void should_subscribe_after_the_permit_is_available() => _client.Received(1).SetDefinitionAndSubscribe(Arg.Any<ReducerDefinition>());
}
